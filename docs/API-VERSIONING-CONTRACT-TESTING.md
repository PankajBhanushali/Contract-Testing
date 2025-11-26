# API Versioning & Backward Compatibility in Contract Testing

## Scenario Overview

### The Evolution

```
Timeline:

SF 17.1 (Legacy)
├── GET /users
└── Returns: All users (v1 response)
    {
      "users": [
        { "id": 1, "name": "John" },
        { "id": 2, "name": "Jane" }
      ]
    }
         │
         │ Provider (VAIS) deployed with v1 endpoint
         │
         ▼
SF 18.1 (Updated)
├── GET /users?apiVersion=2
└── Returns: All users (v2 response with additional fields)
    {
      "users": [
        { "id": 1, "name": "John", "email": "john@company.com", "role": "admin" },
        { "id": 2, "name": "Jane", "email": "jane@company.com", "role": "user" }
      ]
    }

CHALLENGE:
  • SF 17.1 still needs to work with v1 endpoint
  • SF 18.1 wants to use v2 endpoint with new fields
  • VAIS needs to support BOTH simultaneously
  • How to test this contract compatibility?
```

### The Problem Space

```
┌─────────────────────────────────────────────────────────────┐
│                    Contract Testing Challenge                 │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Consumer Evolution          Provider Reality                │
│  ─────────────────          ────────────────                 │
│                                                              │
│  SF 17.1                     VAIS 1.0 (Released)            │
│  ├─ Contract v1              ├─ GET /users                  │
│  └─ Expects: v1 response     └─ Returns: v1 response        │
│                                                              │
│                  ↓ Time passes...                            │
│                                                              │
│  SF 18.1 (Released)          VAIS still at 1.0              │
│  ├─ Contract v2              ├─ GET /users                  │
│  ├─ Expects: v2 response     │  (unchanged)                 │
│  └─ Uses: apiVersion=2       │                              │
│                              │                              │
│  ⚡ PROBLEM ⚡              │                              │
│  SF 17.1 still running!     └─ VAIS 1.5 (Released)         │
│  It uses Contract v1        ├─ GET /users → v1 response    │
│  But provider upgraded      └─ GET /users?apiVersion=2     │
│                                 → v2 response               │
│                                                              │
│  Question:                                                   │
│  How to test both SF 17.1 and SF 18.1 against same VAIS?  │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## Solution Architecture

### Approach 1: Multiple Pact Contracts

```
pacts/
├── SF-v17.1-Consumer-VAIS-Provider.json
│   └── Contract: GET /users (no query params)
│       Response: v1 format (id, name only)
│
└── SF-v18.1-Consumer-VAIS-Provider.json
    └── Contract: GET /users?apiVersion=2
        Response: v2 format (id, name, email, role)
```

### Approach 2: Single Pact with Multiple Scenarios

```
SF-Consumer-VAIS-Provider.json
├── Interaction: "Get users (v1)"
│   └── Request: GET /users
│       Response: v1 format
│
└── Interaction: "Get users (v2)"
    └── Request: GET /users?apiVersion=2
        Response: v2 format
```

### Approach 3: Provider States

```
Provider States:
├── "users endpoint v1 enabled"
│   └── GET /users → returns v1 response
│
└── "users endpoint v2 enabled"
    └── GET /users?apiVersion=2 → returns v2 response
```

---

## Implementation with Pact

### Step 1: Define Multiple Consumer Contracts

#### Consumer Test (SF 17.1)

```csharp
// SF-Consumer-v1-Tests.cs
[Fact]
public void GetUsers_ReturnsV1Format()
{
    Pact.Arrange()
        .Given("users exist in v1 format")
        .UponReceiving("a request for all users (v1)")
        .With(new PactRequest
        {
            Method = HttpMethod.Get,
            Path = "/users",
            // Note: No apiVersion query parameter
        })
        .WillRespondWith(new PactResponse
        {
            Status = 200,
            Headers = new Dictionary<string, string> 
            { 
                { "Content-Type", "application/json" } 
            },
            Body = new
            {
                users = new[]
                {
                    new { id = 1, name = "John" },
                    new { id = 2, name = "Jane" }
                }
            }
        });

    // Act
    var client = GetPactClient();
    var response = await client.GetAsync("/users");
    var content = await response.Content.ReadAsAsync<UsersResponse>();

    // Assert
    Assert.Equal(200, (int)response.StatusCode);
    Assert.Equal(2, content.Users.Count);
    Assert.Equal("John", content.Users[0].Name);
    Assert.Null(content.Users[0].Email); // v1 doesn't have email
}
```

#### Consumer Test (SF 18.1)

```csharp
// SF-Consumer-v2-Tests.cs
[Fact]
public void GetUsers_WithApiVersion2_ReturnsV2Format()
{
    Pact.Arrange()
        .Given("users exist in v2 format with extended fields")
        .UponReceiving("a request for all users with apiVersion=2")
        .With(new PactRequest
        {
            Method = HttpMethod.Get,
            Path = "/users",
            Query = "apiVersion=2"  // Query parameter specifies version
        })
        .WillRespondWith(new PactResponse
        {
            Status = 200,
            Headers = new Dictionary<string, string> 
            { 
                { "Content-Type", "application/json" } 
            },
            Body = new
            {
                users = new[]
                {
                    new 
                    { 
                        id = 1, 
                        name = "John",
                        email = "john@company.com",
                        role = "admin"
                    },
                    new 
                    { 
                        id = 2, 
                        name = "Jane",
                        email = "jane@company.com",
                        role = "user"
                    }
                }
            }
        });

    // Act
    var client = GetPactClient();
    var response = await client.GetAsync("/users?apiVersion=2");
    var content = await response.Content.ReadAsAsync<UsersResponseV2>();

    // Assert
    Assert.Equal(200, (int)response.StatusCode);
    Assert.Equal(2, content.Users.Count);
    Assert.Equal("John", content.Users[0].Name);
    Assert.Equal("john@company.com", content.Users[0].Email);  // v2 has email
    Assert.Equal("admin", content.Users[0].Role);              // v2 has role
}
```

### Step 2: Define Response Models

```csharp
// Models.cs

// V1 Response - Minimal fields
public class UsersResponse
{
    public List<UserV1> Users { get; set; }
}

public class UserV1
{
    public int Id { get; set; }
    public string Name { get; set; }
}

// V2 Response - Extended fields
public class UsersResponseV2
{
    public List<UserV2> Users { get; set; }
}

public class UserV2
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
}
```

### Step 3: Provider Verification with Provider States

#### Provider Test Setup

```csharp
// Provider-Verification-Tests.cs
public class ProviderVerificationTests
{
    private WebApplicationFactory<Startup> _webApplicationFactory;
    private string _pactUri;

    [Fact]
    public async Task VerifyPactV1()
    {
        var config = new PactVerifierConfig
        {
            ProviderVersion = "1.5.0",
            PactBrokerUri = new Uri("https://pact-broker.company.com"),
            BrokerToken = Environment.GetEnvironmentVariable("PACT_BROKER_TOKEN"),
            PublishVerificationResults = true,
            Verbose = true
        };

        var pactVerifier = new PactVerifier(config);

        // Verify the v1 contract
        pactVerifier
            .ServiceProvider("VAIS-Provider", _webApplicationFactory.CreateClient())
            .HonoursPactWith("SF-v17.1-Consumer")
            .PactUri("./pacts/SF-v17.1-Consumer-VAIS-Provider.json")
            .Verify();
    }

    [Fact]
    public async Task VerifyPactV2()
    {
        var config = new PactVerifierConfig
        {
            ProviderVersion = "1.5.0",
            PactBrokerUri = new Uri("https://pact-broker.company.com"),
            BrokerToken = Environment.GetEnvironmentVariable("PACT_BROKER_TOKEN"),
            PublishVerificationResults = true,
            Verbose = true
        };

        var pactVerifier = new PactVerifier(config);

        // Verify the v2 contract
        pactVerifier
            .ServiceProvider("VAIS-Provider", _webApplicationFactory.CreateClient())
            .HonoursPactWith("SF-v18.1-Consumer")
            .PactUri("./pacts/SF-v18.1-Consumer-VAIS-Provider.json")
            .Verify();
    }
}
```

#### Provider Controller - Dual Version Support

```csharp
// Controllers/UsersController.cs
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet]
    public IActionResult GetUsers([FromQuery] string apiVersion = "1")
    {
        if (apiVersion == "2")
        {
            return Ok(GetUsersV2());
        }
        
        return Ok(GetUsersV1());
    }

    private object GetUsersV1()
    {
        return new
        {
            users = new[]
            {
                new { id = 1, name = "John" },
                new { id = 2, name = "Jane" }
            }
        };
    }

    private object GetUsersV2()
    {
        return new
        {
            users = new[]
            {
                new 
                { 
                    id = 1, 
                    name = "John",
                    email = "john@company.com",
                    role = "admin"
                },
                new 
                { 
                    id = 2, 
                    name = "Jane",
                    email = "jane@company.com",
                    role = "user"
                }
            }
        };
    }
}
```

### Step 4: Generate and Publish Pacts

```csharp
// Test Lifecycle
[CollectionDefinition("Pact Collection")]
public class PactCollection : ICollectionFixture<PactFixture>
{
}

[Collection("Pact Collection")]
public class UsersContractTests
{
    private readonly PactFixture _pactFixture;

    public UsersContractTests(PactFixture pactFixture)
    {
        _pactFixture = pactFixture;
    }

    // Tests run here...
    // At end of test execution, pacts are generated and published

    public static void PublishPacts()
    {
        var pactPublisher = new PactPublisher("https://pact-broker.company.com");
        
        // Publish v1 pact
        pactPublisher.PublishAsync(
            consumerName: "SF-v17.1-Consumer",
            providerName: "VAIS-Provider",
            pactFile: "./pacts/SF-v17.1-Consumer-VAIS-Provider.json",
            version: "17.1.0"
        ).Wait();

        // Publish v2 pact
        pactPublisher.PublishAsync(
            consumerName: "SF-v18.1-Consumer",
            providerName: "VAIS-Provider",
            pactFile: "./pacts/SF-v18.1-Consumer-VAIS-Provider.json",
            version: "18.1.0"
        ).Wait();
    }
}
```

---

## Implementation with Postman

### Step 1: Create Multiple Collections

#### Collection: Product-API-Contract-v1.postman_collection.json

```json
{
  "info": {
    "name": "Product-API-Contract-Tests-v1",
    "description": "Contract tests for v1 API (SF 17.1)",
    "_postman_id": "abc123",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "Get All Users - v1",
      "event": [
        {
          "listen": "test",
          "script": {
            "exec": [
              "pm.test('Status code is 200', function () {",
              "  pm.response.to.have.status(200);",
              "});",
              "",
              "pm.test('Response contains users array', function () {",
              "  var jsonData = pm.response.json();",
              "  pm.expect(jsonData).to.have.property('users');",
              "  pm.expect(jsonData.users).to.be.an('array');",
              "});",
              "",
              "pm.test('Each user has id and name (v1 format)', function () {",
              "  var jsonData = pm.response.json();",
              "  jsonData.users.forEach(function(user) {",
              "    pm.expect(user).to.have.property('id');",
              "    pm.expect(user).to.have.property('name');",
              "    pm.expect(user).to.not.have.property('email');",
              "    pm.expect(user).to.not.have.property('role');",
              "  });",
              "});",
              "",
              "pm.test('Response time is less than 500ms', function () {",
              "  pm.expect(pm.response.responseTime).to.be.below(500);",
              "});"
            ]
          }
        }
      ],
      "request": {
        "method": "GET",
        "header": [
          {
            "key": "Accept",
            "value": "application/json"
          }
        ],
        "url": {
          "raw": "{{baseUrl}}/users",
          "host": ["{{baseUrl}}"],
          "path": ["users"]
          // Note: NO apiVersion query parameter for v1
        }
      }
    }
  ]
}
```

#### Collection: Product-API-Contract-v2.postman_collection.json

```json
{
  "info": {
    "name": "Product-API-Contract-Tests-v2",
    "description": "Contract tests for v2 API (SF 18.1)",
    "_postman_id": "def456",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "Get All Users - v2",
      "event": [
        {
          "listen": "test",
          "script": {
            "exec": [
              "pm.test('Status code is 200', function () {",
              "  pm.response.to.have.status(200);",
              "});",
              "",
              "pm.test('Response contains users array', function () {",
              "  var jsonData = pm.response.json();",
              "  pm.expect(jsonData).to.have.property('users');",
              "  pm.expect(jsonData.users).to.be.an('array');",
              "});",
              "",
              "pm.test('Each user has extended fields (v2 format)', function () {",
              "  var jsonData = pm.response.json();",
              "  jsonData.users.forEach(function(user) {",
              "    pm.expect(user).to.have.property('id');",
              "    pm.expect(user).to.have.property('name');",
              "    pm.expect(user).to.have.property('email');",
              "    pm.expect(user).to.have.property('role');",
              "  });",
              "});",
              "",
              "pm.test('Email is valid format', function () {",
              "  var jsonData = pm.response.json();",
              "  var emailRegex = /^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$/;",
              "  jsonData.users.forEach(function(user) {",
              "    pm.expect(user.email).to.match(emailRegex);",
              "  });",
              "});",
              "",
              "pm.test('Role is valid enum value', function () {",
              "  var jsonData = pm.response.json();",
              "  var validRoles = ['admin', 'user', 'guest'];",
              "  jsonData.users.forEach(function(user) {",
              "    pm.expect(validRoles).to.include(user.role);",
              "  });",
              "});",
              "",
              "pm.test('Response time is less than 500ms', function () {",
              "  pm.expect(pm.response.responseTime).to.be.below(500);",
              "});"
            ]
          }
        }
      ],
      "request": {
        "method": "GET",
        "header": [
          {
            "key": "Accept",
            "value": "application/json"
          }
        ],
        "url": {
          "raw": "{{baseUrl}}/users?apiVersion=2",
          "host": ["{{baseUrl}}"],
          "path": ["users"],
          "query": [
            {
              "key": "apiVersion",
              "value": "2"
            }
          ]
        }
      }
    }
  ]
}
```

### Step 2: Environments for Different Versions

#### Environment: Product-API-Environment-v1.postman_environment.json

```json
{
  "id": "abc-123",
  "name": "Product-API-Environment-v1",
  "values": [
    {
      "key": "baseUrl",
      "value": "http://localhost:5001",
      "enabled": true
    },
    {
      "key": "apiVersion",
      "value": "1",
      "enabled": true
    },
    {
      "key": "consumerName",
      "value": "SF-v17.1-Consumer",
      "enabled": true
    }
  ]
}
```

#### Environment: Product-API-Environment-v2.postman_environment.json

```json
{
  "id": "def-456",
  "name": "Product-API-Environment-v2",
  "values": [
    {
      "key": "baseUrl",
      "value": "http://localhost:5001",
      "enabled": true
    },
    {
      "key": "apiVersion",
      "value": "2",
      "enabled": true
    },
    {
      "key": "consumerName",
      "value": "SF-v18.1-Consumer",
      "enabled": true
    }
  ]
}
```

### Step 3: Newman Commands

```bash
# Test v1 contracts (SF 17.1)
newman run \
  Product-API-Contract-v1.postman_collection.json \
  -e Product-API-Environment-v1.postman_environment.json \
  -r json,html \
  --reporter-json-export results-v1.json \
  --reporter-html-export results-v1.html

# Test v2 contracts (SF 18.1)
newman run \
  Product-API-Contract-v2.postman_collection.json \
  -e Product-API-Environment-v2.postman_environment.json \
  -r json,html \
  --reporter-json-export results-v2.json \
  --reporter-html-export results-v2.html
```

---

## CI/CD Pipeline Strategy

### Azure Pipelines Implementation

#### Strategy 1: Separate Matrix Jobs

```yaml
# azure-pipelines.yml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

variables:
  dotnetVersion: '8.0.x'

stages:
  - stage: Test
    jobs:
      # Test v1 contracts
      - job: TestContractsV1
        displayName: 'Test v1 Contracts (SF 17.1)'
        steps:
          - checkout: self

          - task: DotNetCoreCLI@2
            inputs:
              command: 'test'
              projects: '**/Consumer.Tests.csproj'
              arguments: '--filter "Category=ContractV1"'
            displayName: 'Run Consumer v1 Pact Tests'

          - task: PublishBuildArtifacts@1
            inputs:
              PathtoPublish: '$(Build.SourcesDirectory)/pacts'
              ArtifactName: 'pacts-v1'

      # Test v2 contracts
      - job: TestContractsV2
        displayName: 'Test v2 Contracts (SF 18.1)'
        steps:
          - checkout: self

          - task: DotNetCoreCLI@2
            inputs:
              command: 'test'
              projects: '**/Consumer.Tests.csproj'
              arguments: '--filter "Category=ContractV2"'
            displayName: 'Run Consumer v2 Pact Tests'

          - task: PublishBuildArtifacts@1
            inputs:
              PathtoPublish: '$(Build.SourcesDirectory)/pacts'
              ArtifactName: 'pacts-v2'

      # Verify both contracts against provider
      - job: VerifyContracts
        displayName: 'Verify Contracts Against Provider'
        dependsOn:
          - TestContractsV1
          - TestContractsV2
        steps:
          - checkout: self

          # Start provider
          - task: DotNetCoreCLI@2
            inputs:
              command: 'build'
              projects: '**/Provider.csproj'
            displayName: 'Build Provider'

          # Download v1 pacts
          - task: DownloadBuildArtifacts@0
            inputs:
              artifactName: 'pacts-v1'
              downloadPath: '$(System.ArtifactsDirectory)'

          # Download v2 pacts
          - task: DownloadBuildArtifacts@0
            inputs:
              artifactName: 'pacts-v2'
              downloadPath: '$(System.ArtifactsDirectory)'

          # Verify v1
          - task: DotNetCoreCLI@2
            inputs:
              command: 'test'
              projects: '**/Provider.Tests.csproj'
              arguments: '--filter "Category=VerifyV1" --configuration Release'
            displayName: 'Verify v1 Contracts'

          # Verify v2
          - task: DotNetCoreCLI@2
            inputs:
              command: 'test'
              projects: '**/Provider.Tests.csproj'
              arguments: '--filter "Category=VerifyV2" --configuration Release'
            displayName: 'Verify v2 Contracts'

          - task: PublishTestResults@2
            inputs:
              testResultsFormat: 'VSTest'
              testResultsFiles: '**/TEST-*.xml'
              mergeTestResults: true
```

#### Strategy 2: Postman Collections with Newman

```yaml
# azure-pipelines-postman.yml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
  - checkout: self

  - task: NodeTool@0
    inputs:
      versionSpec: '18.x'
    displayName: 'Install Node.js'

  - script: npm install -g newman
    displayName: 'Install Newman'

  - script: |
      # Start provider service
      dotnet build Provider/src/provider.csproj
      dotnet run --project Provider/src/provider.csproj &
      PROVIDER_PID=$!
      
      # Wait for service to start
      sleep 5
      
      # Run v1 contract tests
      newman run \
        postman-collections/Product-API-Contract-v1.postman_collection.json \
        -e postman-collections/Product-API-Environment-v1.postman_environment.json \
        --reporters cli,json \
        --reporter-json-export results-v1.json
      
      RESULTS_V1=$?
      
      # Run v2 contract tests
      newman run \
        postman-collections/Product-API-Contract-v2.postman_collection.json \
        -e postman-collections/Product-API-Environment-v2.postman_environment.json \
        --reporters cli,json \
        --reporter-json-export results-v2.json
      
      RESULTS_V2=$?
      
      # Stop provider
      kill $PROVIDER_PID
      
      # Exit with failure if any tests failed
      if [ $RESULTS_V1 -ne 0 ] || [ $RESULTS_V2 -ne 0 ]; then
        exit 1
      fi
    displayName: 'Run Postman Contract Tests (v1 & v2)'

  - task: PublishBuildArtifacts@1
    inputs:
      PathtoPublish: '$(Build.SourcesDirectory)'
      ArtifactName: 'newman-results'
    condition: always()
```

---

## Compatibility Matrix & Documentation

### Version Compatibility Chart

```
┌─────────────┬──────────────────┬──────────────────┬─────────────┐
│ Consumer    │ Provider Version │ Endpoint         │ Status      │
├─────────────┼──────────────────┼──────────────────┼─────────────┤
│ SF 17.1     │ VAIS 1.0 - 1.4   │ GET /users       │ ✅ Working  │
│ SF 17.1     │ VAIS 1.5+        │ GET /users       │ ✅ Working  │
│ SF 17.1     │ VAIS 1.5+        │ GET /users?v=2   │ ❌ N/A      │
├─────────────┼──────────────────┼──────────────────┼─────────────┤
│ SF 18.1     │ VAIS 1.0 - 1.4   │ GET /users       │ ❌ Fail     │
│ SF 18.1     │ VAIS 1.0 - 1.4   │ GET /users?v=2   │ ❌ N/A      │
│ SF 18.1     │ VAIS 1.5+        │ GET /users?v=2   │ ✅ Working  │
└─────────────┴──────────────────┴──────────────────┴─────────────┘
```

### Deployment Strategy

```
Timeline:

Phase 1: Support v1 Only (Current State)
├── Provider Version: 1.0
├── Endpoints: GET /users (v1)
├── Consumers: SF 17.1 ✅

Phase 2: Add v2, Keep v1 (Transition)
├── Provider Version: 1.5
├── Endpoints: 
│   ├── GET /users (v1) ✅
│   └── GET /users?apiVersion=2 (v2) ✅
├── Consumers: 
│   ├── SF 17.1 ✅
│   └── SF 18.1 ✅

Phase 3: Deprecate v1 (Future - not shown here)
├── Provider Version: 2.0
├── Endpoints: GET /users?apiVersion=2 (v2 only)
├── Consumers: SF 18.1+ only ✅
└── Timeline: 2-3 quarters after Phase 2
```

### Test Coverage Map

```
Test Scope Analysis:

┌──────────────────────────────────────────────────┐
│ Consumer Contract Tests (Generate Pacts)          │
├──────────────────────────────────────────────────┤
│                                                  │
│ SF 17.1 Contract Tests                          │
│ ├─ GET /users (no params)                       │
│ ├─ Expect: 200, users array, v1 schema          │
│ └─ Pact: SF-v17.1-Consumer-VAIS-Provider.json   │
│                                                  │
│ SF 18.1 Contract Tests                          │
│ ├─ GET /users?apiVersion=2                      │
│ ├─ Expect: 200, users array, v2 schema          │
│ └─ Pact: SF-v18.1-Consumer-VAIS-Provider.json   │
│                                                  │
└──────────────────────────────────────────────────┘
                      ↓
┌──────────────────────────────────────────────────┐
│ Provider Verification Tests                      │
├──────────────────────────────────────────────────┤
│                                                  │
│ Verify SF 17.1 Pact                             │
│ ├─ Run provider on http://localhost:5001        │
│ ├─ Check: GET /users returns v1 format          │
│ └─ Result: ✅ Passed                            │
│                                                  │
│ Verify SF 18.1 Pact                             │
│ ├─ Run provider on http://localhost:5001        │
│ ├─ Check: GET /users?apiVersion=2 returns v2    │
│ └─ Result: ✅ Passed                            │
│                                                  │
└──────────────────────────────────────────────────┘
```

---

## Key Considerations

### ✅ DO

1. **Create separate contracts per major version**
   ```csharp
   // Clear naming
   SF-v17.1-Consumer-VAIS-Provider.json
   SF-v18.1-Consumer-VAIS-Provider.json
   ```

2. **Version your response models**
   ```csharp
   public class UserV1 { /* v1 fields */ }
   public class UserV2 { /* v1 + v2 fields */ }
   ```

3. **Test backward compatibility explicitly**
   ```csharp
   [Trait("Category", "BackwardCompatibility")]
   public void VerifyV1ContractStillWorks() { }
   ```

4. **Document endpoint evolution**
   ```markdown
   # Users Endpoint Evolution
   
   - v1: GET /users (SF 17.1 and earlier)
   - v2: GET /users?apiVersion=2 (SF 18.1+)
   ```

5. **Track version in CI/CD artifacts**
   ```yaml
   ArtifactName: 'pacts-v1'
   ArtifactName: 'pacts-v2'
   ```

### ❌ DON'T

1. **Don't remove old endpoint support**
   - Keep v1 working while v2 is being adopted
   - Remove only after deprecation period

2. **Don't reuse same contract file**
   ```csharp
   // Bad: Single pact for both versions
   SF-Consumer-VAIS-Provider.json
   
   // Good: Separate contracts
   SF-v17.1-Consumer-VAIS-Provider.json
   SF-v18.1-Consumer-VAIS-Provider.json
   ```

3. **Don't forget to test both paths**
   ```csharp
   // Both must pass
   ✅ GET /users → v1 response
   ✅ GET /users?apiVersion=2 → v2 response
   ```

4. **Don't change v1 response format**
   - Maintain exact same schema
   - Only add optional fields in v2

5. **Don't skip deprecation communication**
   - Document sunset dates
   - Notify consumers ahead of time

---

## Common Pitfalls & Solutions

### Pitfall 1: Response Format Mismatch

**Problem**:
```csharp
// v1 contract expects
{ "id": 1, "name": "John" }

// But provider returns
{ "id": 1, "name": "John", "email": null }  // Extra null field!
```

**Solution**:
```csharp
// Use explicit models without null handling
public class UserV1
{
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public int Id { get; set; }
    
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }
    // No email field
}

public class UserV2
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
}
```

### Pitfall 2: Query Parameter Not Recognized

**Problem**:
```
Consumer calls: GET /users?apiVersion=2
Provider receives: GET /users (query param ignored!)
```

**Solution**:
```csharp
[HttpGet]
public IActionResult GetUsers(
    [FromQuery(Name = "apiVersion")] string apiVersion = "1")
{
    // Explicit query parameter binding
    if (apiVersion == "2")
        return Ok(GetUsersV2());
    
    return Ok(GetUsersV1());
}
```

### Pitfall 3: Contract Versioning Confusion

**Problem**:
```
Pact files don't indicate which API version they test:
- ApiClient-ProductService.json (which version?)
- ProductService-ApiClient.json (ambiguous)
```

**Solution**:
```
Use clear naming with version indicators:
- SF-v17.1-Consumer-VAIS-Provider-v1-api.json
- SF-v18.1-Consumer-VAIS-Provider-v2-api.json

Or semantic versioning:
- Pacts/Consumer-17.1/VAIS-Provider.json
- Pacts/Consumer-18.1/VAIS-Provider.json
```

---

## Rollout Checklist

- [ ] Create v2 contract test in consumer (SF 18.1)
- [ ] Generate v2 pact file
- [ ] Publish v2 pact to Pact Broker
- [ ] Implement v2 endpoint in provider (GET /users?apiVersion=2)
- [ ] Add provider verification test for v2
- [ ] Verify both v1 and v2 tests pass
- [ ] Deploy provider with dual support (v1 + v2)
- [ ] Test SF 17.1 against new provider (backward compat)
- [ ] Test SF 18.1 against new provider (new feature)
- [ ] Update documentation with compatibility matrix
- [ ] Communicate deprecation timeline for v1
- [ ] Monitor production for both endpoints
- [ ] Track migration progress of consumers from v1 → v2

---

**Last Updated**: November 26, 2025
