# Postman Contract Testing with OAuth & In-Memory vs Deployed Providers

## Table of Contents

1. [OAuth Flow in Contract Testing](#oauth-flow-in-contract-testing)
2. [Bearer Token Implementation](#bearer-token-implementation)
3. [OAuth Authorization Code Flow](#oauth-authorization-code-flow)
4. [OAuth Implicit Flow](#oauth-implicit-flow)
5. [In-Memory vs Deployed Providers](#in-memory-vs-deployed-providers)
6. [Best Practices](#best-practices)
7. [Real-World Examples](#real-world-examples)

---

## OAuth Flow in Contract Testing

### Overview

When your Provider API is secured with OAuth, contract tests need to authenticate before testing the endpoints. There are several approaches:

### Types of OAuth Flows

| Flow Type | Use Case | Security Level |
|-----------|----------|-----------------|
| **Authorization Code** | Web apps, mobile | Highest |
| **Implicit** | SPAs (deprecated) | Medium |
| **Client Credentials** | Service-to-service | High |
| **Resource Owner Password** | Legacy apps, testing | Low (Not for production) |
| **Bearer Token (Manual)** | Testing, development | Medium |

**For Contract Testing**: Use **Client Credentials** or **Bearer Token (Manual)**

---

## Bearer Token Implementation

### Simplest Approach: Static Bearer Token

#### Step 1: Create Auth Header in Postman

In your Postman collection, under **Pre-request Script** tab:

```javascript
// Option 1: Hardcoded token (for testing only)
const token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";
pm.environment.set("bearerToken", token);

// Option 2: Use environment variable
// pm.environment.set("bearerToken", pm.environment.get("oauth_token"));
```

#### Step 2: Use Token in Headers

In request **Headers** tab:

```
Key: Authorization
Value: Bearer {{bearerToken}}
```

#### Step 3: Add to Environment File

**Product-API-Environment.postman_environment.json**:

```json
{
  "id": "env-id",
  "name": "Product-API-Environment",
  "values": [
    {
      "key": "baseUrl",
      "value": "http://localhost:5001"
    },
    {
      "key": "bearerToken",
      "value": "your-test-token-here"
    }
  ]
}
```

#### Step 4: Run Collection with Token

```bash
newman run \
  Product-API-Contract-Tests.postman_collection.json \
  -e Product-API-Environment.postman_environment.json \
  -r json \
  --reporter-json-export results.json
```

---

## OAuth Authorization Code Flow

### For More Secure Testing

#### Step 1: Obtain Token Before Tests

**Pre-request Script (at collection level)**:

```javascript
// Function to get OAuth token
const getOAuthToken = (callback) => {
  // OAuth endpoint
  const authUrl = pm.environment.get("auth_url");
  const clientId = pm.environment.get("client_id");
  const clientSecret = pm.environment.get("client_secret");
  const redirectUri = pm.environment.get("redirect_uri");

  // Request auth code
  const request = {
    url: authUrl + "/oauth/authorize",
    method: "GET",
    header: {
      "Content-Type": "application/x-www-form-urlencoded"
    },
    body: {
      mode: "urlencoded",
      urlencoded: [
        { key: "client_id", value: clientId },
        { key: "redirect_uri", value: redirectUri },
        { key: "response_type", value: "code" },
        { key: "scope", value: "read write" }
      ]
    }
  };

  pm.sendRequest(request, (err, response) => {
    if (err) {
      console.log(err);
    } else {
      // Get auth code from redirect URL
      const authCode = response.headers.get("Location").split("code=")[1];
      
      // Exchange code for token
      const tokenRequest = {
        url: authUrl + "/oauth/token",
        method: "POST",
        header: {
          "Content-Type": "application/x-www-form-urlencoded"
        },
        body: {
          mode: "urlencoded",
          urlencoded: [
            { key: "grant_type", value: "authorization_code" },
            { key: "code", value: authCode },
            { key: "client_id", value: clientId },
            { key: "client_secret", value: clientSecret },
            { key: "redirect_uri", value: redirectUri }
          ]
        }
      };

      pm.sendRequest(tokenRequest, (err, response) => {
        if (err) {
          console.log(err);
        } else {
          const jsonResponse = response.json();
          pm.environment.set("access_token", jsonResponse.access_token);
          callback();
        }
      });
    }
  });
};

// Execute at collection level
getOAuthToken(() => {
  console.log("Token acquired");
});
```

#### Step 2: Use Token in Requests

```javascript
// In request header
{
  "Authorization": "Bearer {{access_token}}"
}
```

---

## OAuth Client Credentials Flow

### Best Practice for Service-to-Service Testing

#### Step 1: Configure OAuth Server

```csharp
// Your OAuth server should support Client Credentials
// Example: IdentityServer4, Azure AD B2C, Auth0

// Configuration:
// - Client ID: "postman-client"
// - Client Secret: "secret-key"
// - Grant Type: "client_credentials"
// - Scope: "api"
```

#### Step 2: Pre-request Script to Get Token

```javascript
// Get access token using Client Credentials flow
const getAccessToken = (callback) => {
  const request = {
    url: pm.environment.get("auth_url") + "/oauth/token",
    method: "POST",
    header: {
      "Content-Type": "application/x-www-form-urlencoded"
    },
    body: {
      mode: "urlencoded",
      urlencoded: [
        { 
          key: "grant_type", 
          value: "client_credentials" 
        },
        { 
          key: "client_id", 
          value: pm.environment.get("client_id") 
        },
        { 
          key: "client_secret", 
          value: pm.environment.get("client_secret") 
        },
        { 
          key: "scope", 
          value: "api" 
        }
      ]
    }
  };

  pm.sendRequest(request, (err, response) => {
    if (err) {
      console.error("Token request failed:", err);
      callback(null);
    } else {
      const jsonResponse = response.json();
      const token = jsonResponse.access_token;
      const expiresIn = jsonResponse.expires_in;
      
      // Set token and expiry
      pm.environment.set("access_token", token);
      pm.environment.set("token_expiry", new Date().getTime() + (expiresIn * 1000));
      
      console.log("Token acquired, expires in " + expiresIn + " seconds");
      callback(token);
    }
  });
};

// Check if token exists and is still valid
if (!pm.environment.get("access_token") || 
    new Date().getTime() > pm.environment.get("token_expiry")) {
  getAccessToken((token) => {
    if (!token) {
      throw new Error("Failed to obtain access token");
    }
  });
}
```

#### Step 3: Environment Configuration

```json
{
  "id": "env-oauth",
  "name": "OAuth Environment",
  "values": [
    {
      "key": "baseUrl",
      "value": "http://localhost:5001"
    },
    {
      "key": "auth_url",
      "value": "http://localhost:5000"
    },
    {
      "key": "client_id",
      "value": "postman-client"
    },
    {
      "key": "client_secret",
      "value": "your-secret-key"
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

## OAuth Implicit Flow

### For SPA Contract Testing (Deprecated but Still Used)

```javascript
// Implicit flow - get token directly (no auth code exchange)
const getImplicitToken = (callback) => {
  const request = {
    url: pm.environment.get("auth_url") + "/oauth/authorize",
    method: "GET",
    header: {},
    url: pm.environment.get("auth_url") + "/oauth/authorize?response_type=token&client_id=" +
      pm.environment.get("client_id") + "&redirect_uri=" + 
      encodeURIComponent(pm.environment.get("redirect_uri")) +
      "&scope=read write"
  };

  pm.sendRequest(request, (err, response) => {
    if (err) {
      console.log(err);
    } else {
      // Extract token from redirect URL fragment
      const location = response.headers.get("Location");
      const token = location.split("access_token=")[1].split("&")[0];
      pm.environment.set("access_token", token);
      callback(token);
    }
  });
};
```

---

## In-Memory vs Deployed Providers

### Key Comparison

| Aspect | In-Memory | Deployed |
|--------|-----------|----------|
| **Startup Time** | Fast (milliseconds) | Slow (seconds/minutes) |
| **Isolation** | Complete | Network-dependent |
| **Reliability** | Very high | Depends on infrastructure |
| **Resource Usage** | Minimal | Higher |
| **Suited for** | Local dev, CI/CD | Staging/Production testing |
| **Test Scope** | Unit-like | Integration-like |
| **Setup Complexity** | Low | High |
| **Debugging** | Easy | Harder |

---

## In-Memory Approach (RECOMMENDED for Contract Tests)

### What is In-Memory?

Running your API server as part of the test process:
- No separate deployment needed
- Server starts before tests
- Server stops after tests
- Only process: Test runner

### Implementation in Postman + Newman

#### Step 1: Create Test Startup Script

**C# Example - Start API in-memory**:

```csharp
// test-runner.cs or in your test project
using System;
using System.Diagnostics;
using System.Threading.Tasks;

public class ContractTestRunner
{
    private static Process apiProcess;

    public static async Task Main(string[] args)
    {
        try
        {
            // Start API in-memory
            Console.WriteLine("Starting API server...");
            StartApiServer();
            
            // Wait for API to be ready
            await WaitForApiReady();
            
            // Run Postman collection tests
            Console.WriteLine("Running contract tests...");
            RunPostmanTests();
            
            // Stop API server
            StopApiServer();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            StopApiServer();
            Environment.Exit(1);
        }
    }

    private static void StartApiServer()
    {
        apiProcess = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run",
            WorkingDirectory = "path/to/Provider/src",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        Process.Start(apiProcess);
    }

    private static async Task WaitForApiReady()
    {
        // Poll API until it responds
        for (int i = 0; i < 30; i++)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync("http://localhost:5001/api/products");
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("API is ready");
                        return;
                    }
                }
            }
            catch { }
            
            await Task.Delay(1000);
        }
        
        throw new Exception("API failed to start");
    }

    private static void RunPostmanTests()
    {
        var process = Process.Start("newman", 
            "run contract-tests.json -e environment.json");
        process.WaitForExit();
    }

    private static void StopApiServer()
    {
        apiProcess?.Kill();
        apiProcess?.Dispose();
    }
}
```

#### Step 2: GitHub Actions Workflow (In-Memory)

```yaml
name: Postman Contract Tests - In-Memory

on: [push, pull_request]

jobs:
  contract-tests:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: '18'

      - name: Install Newman
        run: npm install -g newman

      - name: Restore dependencies
        run: dotnet restore postman-contract-testing/Provider/src/provider.csproj

      - name: Start API in-memory
        run: |
          cd postman-contract-testing/Provider/src
          dotnet run &
          sleep 5

      - name: Wait for API to be ready
        run: |
          for i in {1..30}; do
            curl -f http://localhost:5001/api/products && break
            echo "Waiting for API... ($i/30)"
            sleep 1
          done

      - name: Run Postman collection
        run: |
          newman run \
            "postman-contract-testing/postman-collections/Product-API-Contract-Tests.postman_collection.json" \
            -e "postman-contract-testing/postman-collections/Product-API-Environment.postman_environment.json" \
            --reporters cli,json \
            --reporter-json-export results.json

      - name: Upload results
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: test-results
          path: results.json
```

---

## Deployed Approach

### When to Use

- ✅ Testing against actual deployment
- ✅ Validating infrastructure setup
- ✅ Testing database interactions
- ✅ Performance/load testing
- ✅ Staging environment validation

### Implementation

#### Step 1: Deploy to Staging

```yaml
name: Deploy and Test

on: [push]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Deploy to staging
        run: |
          kubectl apply -f deployment.yaml
          kubectl set image deployment/api api=myrepo/api:${{ github.sha }}
          kubectl rollout status deployment/api

  test:
    needs: deploy
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: '18'

      - name: Install Newman
        run: npm install -g newman

      - name: Run tests against deployed API
        run: |
          newman run contract-tests.json \
            -e staging-environment.json
```

#### Step 2: Staging Environment Config

```json
{
  "id": "staging-env",
  "name": "Staging Environment",
  "values": [
    {
      "key": "baseUrl",
      "value": "https://api-staging.example.com"
    },
    {
      "key": "bearerToken",
      "value": "staging-test-token"
    }
  ]
}
```

---

## Best Practices

### ✅ DO

1. **Use In-Memory for Contract Tests**
   - Faster feedback
   - Isolated from infrastructure
   - Deterministic results
   - Better for CI/CD

2. **Implement Proper OAuth Flow**
   ```javascript
   // Get token once at collection level
   // Refresh if expired
   // Use environment variables
   ```

3. **Separate Auth from API Testing**
   ```javascript
   // Pre-request: Get token
   // Request: Use token in header
   // Test: Validate response
   ```

4. **Use Staging for Integration Tests**
   - Separate from contract tests
   - Use different environment files
   - Test with real databases

5. **Mock External Dependencies**
   - OAuth server in-memory
   - Database with test data
   - External services with stubs

6. **Version Token Strategy**
   ```javascript
   // Token management
   - Get token if not exists
   - Refresh if expired
   - Store in environment
   ```

### ❌ DON'T

1. **Don't hardcode tokens in collection**
   - Use environment variables
   - Rotate tokens regularly
   - Never commit secrets

2. **Don't test against production**
   - Use staging/dev environments
   - Production for smoke tests only
   - Protect production data

3. **Don't rely on external OAuth**
   - Mock in-memory when possible
   - Reduces test flakiness
   - Speeds up execution

4. **Don't skip error cases**
   - Test 401 Unauthorized
   - Test 403 Forbidden
   - Test token expiry

5. **Don't assume token stays valid**
   - Always check expiry
   - Refresh before use
   - Handle token errors

---

## Real-World Examples

### Example 1: OAuth + In-Memory Provider

**Architecture**:
```
CI/CD Pipeline
    ↓
1. Start OAuth Server (in-memory)
2. Start API Server (in-memory)
3. Get token from OAuth server
4. Run Postman tests with token
5. Validate responses
6. Stop both servers
```

**Workflow**:
```yaml
# GitHub Actions
- Start in-memory OAuth server
  Command: dotnet run --project OAuthServer.csproj &

- Start API server with OAuth validation
  Command: dotnet run --project Provider.csproj &

- Get token from OAuth
  POST http://localhost:5000/oauth/token

- Run Postman tests with token
  newman run collection.json -e env.json

- Tests validate:
  - Status 200 OK
  - Valid response schema
  - Data returned correctly
```

### Example 2: Deployed Provider + OAuth

**Architecture**:
```
CI/CD Pipeline
    ↓
1. Deploy to staging environment
2. Get token from staging OAuth
3. Run Postman tests against staging API
4. Report results
5. Optionally deploy to production
```

**Environment Config**:
```json
{
  "values": [
    { "key": "baseUrl", "value": "https://api-staging.company.com" },
    { "key": "auth_url", "value": "https://auth-staging.company.com" },
    { "key": "client_id", "value": "staging-client" },
    { "key": "client_secret", "value": "{{STAGING_CLIENT_SECRET}}" }
  ]
}
```

### Example 3: Local Development with OAuth

**Setup**:
```bash
# Terminal 1: Start local OAuth server
dotnet run --project OAuthServer

# Terminal 2: Start local API
dotnet run --project Provider

# Terminal 3: Run Postman tests
newman run contract-tests.json -e local-environment.json
```

**Local Environment**:
```json
{
  "values": [
    { "key": "baseUrl", "value": "http://localhost:5001" },
    { "key": "auth_url", "value": "http://localhost:5000" },
    { "key": "client_id", "value": "local-client" },
    { "key": "client_secret", "value": "local-secret" }
  ]
}
```

---

## Token Refresh Strategy

### Automatic Token Refresh in Tests

```javascript
// Collection-level pre-request script
const shouldRefreshToken = () => {
  const tokenExpiry = pm.environment.get("token_expiry");
  const now = new Date().getTime();
  
  // Refresh if no token or expires in less than 5 minutes
  return !tokenExpiry || (tokenExpiry - now) < 300000;
};

if (shouldRefreshToken()) {
  const authUrl = pm.environment.get("auth_url");
  const clientId = pm.environment.get("client_id");
  const clientSecret = pm.environment.get("client_secret");

  const request = {
    url: authUrl + "/oauth/token",
    method: "POST",
    header: {
      "Content-Type": "application/x-www-form-urlencoded"
    },
    body: {
      mode: "urlencoded",
      urlencoded: [
        { key: "grant_type", value: "client_credentials" },
        { key: "client_id", value: clientId },
        { key: "client_secret", value: clientSecret }
      ]
    }
  };

  pm.sendRequest(request, (err, response) => {
    if (err) {
      console.error("Token refresh failed:", err);
    } else {
      const jsonResponse = response.json();
      pm.environment.set("access_token", jsonResponse.access_token);
      pm.environment.set("token_expiry", 
        new Date().getTime() + (jsonResponse.expires_in * 1000)
      );
    }
  });
}
```

---

## Summary

### For Contract Testing (Recommended Setup)

1. **Provider**: In-Memory (fast, isolated)
2. **OAuth**: In-Memory with test credentials
3. **Token Management**: Automatic refresh in pre-request
4. **Environment**: Use separate configs for local/staging/prod
5. **CI/CD**: Automated with Newman CLI

### For Integration Testing

1. **Provider**: Deployed to staging
2. **OAuth**: Real OAuth server or mock
3. **Token Management**: Get fresh token for each test run
4. **Environment**: Production-like configuration
5. **CI/CD**: Trigger after deployment

### Key Takeaway

**Use in-memory providers for contract tests** to keep feedback loops fast and reduce dependencies. Use deployed providers for integration and staging tests to validate real-world scenarios.

---

**Last Updated**: November 26, 2025
