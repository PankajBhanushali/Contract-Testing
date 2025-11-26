# Contract Testing: Pact vs Postman
## A Comprehensive Comparison

---

## Slide 1: Introduction to Contract Testing

### What is Contract Testing?
- **Definition**: Testing the interface (contract) between services to ensure they can communicate correctly
- **Purpose**: Catch integration issues early without running full integration tests
- **Analogy**: Like a legal contract - both parties agree on terms before doing business

### The Problem We're Solving
- **Traditional Integration Tests**:
  - Slow and brittle
  - Require all services to be running
  - Hard to maintain
  - Often test too much (happy path only)
  
- **Unit Tests Aren't Enough**:
  - Can't verify actual API compatibility
  - Consumer may have wrong assumptions about provider

---

## Slide 2: The Testing Pyramid & Where Contract Testing Fits

```
        /\
       /  \      E2E Tests (Slow, Brittle)
      /    \
     /------\    Integration Tests (Medium)
    /        \   👉 CONTRACT TESTS (Fast, Focused)
   /----------\  Unit Tests (Fast, Many)
  /____________\
```

**Contract Testing Sweet Spot**:
- Faster than integration tests
- More confidence than unit tests
- Validates real API interactions
- Runs in isolation

---

## Slide 3: Contract Testing Solutions Landscape

### Popular Tools & Approaches

1. **Consumer-Driven Contract Testing**
   - **Pact** (Most popular, multi-language)
   - Spring Cloud Contract

2. **API-First / Specification-Based**
   - **OpenAPI/Swagger validation** (Spec-driven)
   - **Specmatic** (Spec-driven with auto test generation)
   - JSON Schema validators
   - Dredd, Prism, Schemathesis

3. **Collection/Manual Testing**
   - Postman Contract Testing
   - Manual test scripts

4. **Record & Replay**
   - VCR/WireMock
   - Traffic recording tools

**Today's Focus**: Four Approaches
- **Pact**: Consumer-driven contracts
- **Postman**: Collection-based testing
- **OpenAPI**: Specification-driven validation
- **Specmatic**: Automatic spec-driven testing with mock servers

---

## Slide 4: Pact - Consumer-Driven Contract Testing

### Core Philosophy
> "The consumer defines what they need from the provider"

### How It Works
1. **Consumer writes tests** describing expected interactions
2. **Pact file generated** (JSON contract)
3. **Provider verifies** they can satisfy the contract
4. **Pact Broker** manages contract versions and verification results

### Key Benefits
- Consumer and provider can develop in parallel
- Contracts generated from real code
- Built-in mock server for consumer testing
- Version compatibility checking
- Provider states for different scenarios

---

## Slide 5: Pact Workflow - Visual

```
┌──────────────┐                    ┌──────────────┐
│   CONSUMER   │                    │   PROVIDER   │
└──────────────┘                    └──────────────┘
       │                                   │
       │ 1. Write Pact test                │
       │    (what I expect)                │
       ▼                                   │
  ┌─────────┐                             │
  │ Pact    │ 2. Generate                  │
  │ File    │────────────────────────────▶│
  │ (JSON)  │    contract                  │
  └─────────┘                             │
       │                                   │
       │                            3. Verify contract
       │                            4. Run provider tests
       │                                   │
       │                            ┌──────▼──────┐
       │                            │  ✓ PASS     │
       │◀───────────────────────────│  ✗ FAIL     │
       │     5. Results              └─────────────┘
       │
  ┌────▼─────┐
  │  Pact    │  6. Publish results
  │  Broker  │     & contracts
  └──────────┘
```

---

## Slide 6: Pact Example - Consumer Side

### Consumer Test (C#)
```csharp
[Fact]
public async Task GetProduct()
{
    // Define expected interaction
    pact.UponReceiving("A valid request for a product")
        .Given("product with ID 10 exists")
        .WithRequest(HttpMethod.Get, "/api/products/10")
        .WithHeader("Authorization", Match.Regex("Bearer ...", pattern))
    .WillRespond()
        .WithStatus(HttpStatusCode.OK)
        .WithHeader("Content-Type", "application/json")
        .WithJsonBody(new TypeMatcher(expectedProduct));

    // Execute against mock server
    await pact.VerifyAsync(async ctx => {
        var response = await ApiClient.GetProduct(10);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    });
}
```

**Output**: Pact file with contract specifications

---

## Slide 7: Pact Example - Provider Side

### Provider Verification Test
```csharp
[Fact]
public void EnsureProviderHonoursPactWithConsumer()
{
    using (var webHost = WebHost.CreateDefaultBuilder()
        .UseStartup<TestStartup>()
        .UseUrls(serviceUri)
        .Build())
    {
        webHost.Start();

        IPactVerifier verifier = new PactVerifier("ProductService", config);
        verifier
            .WithHttpEndpoint(new Uri(serviceUri))
            .WithFileSource(pactFile)
            .WithProviderStateUrl(new Uri($"{serviceUri}/provider-states"))
            .Verify();
    }
}
```

**Key Features**:
- Provider states (data setup)
- Request filters (auth tokens)
- Automatic verification

---

## Slide 8: Postman Contract Testing

### Approach
> "Define and validate API contracts through comprehensive test scripts"

### How It Works
1. **Create Postman collection** with contract expectations
2. **Write test scripts** to validate response structure
3. **Share collection** with team/CI pipeline
4. **Run with Newman CLI** in automation
5. **Validate against running API**

### Key Benefits
- No special frameworks needed
- Visual interface for debugging
- Team already knows Postman
- Can test running services
- Includes API documentation

---

## Slide 9: Postman Contract Test Example

### Contract Test Script
```javascript
// Get Product by ID - Contract Test
pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});

pm.test("Product has required fields with correct types", function () {
    const product = pm.response.json();
    pm.expect(product).to.have.property('id').that.is.a('number');
    pm.expect(product).to.have.property('name').that.is.a('string');
    pm.expect(product).to.have.property('type').that.is.a('string');
    pm.expect(product).to.have.property('version').that.is.a('string');
});

pm.test("Product ID matches requested ID", function () {
    const product = pm.response.json();
    pm.expect(product.id).to.equal(10);
});
```

**Run with Newman**:
```bash
newman run contract-tests.json -e environment.json
```

---

## Slide 10: OpenAPI - Specification-Driven Contract Testing

### Approach
> "Define contract as OpenAPI spec, validate both consumer and provider against it"

### How It Works
1. **Create OpenAPI specification** (openapi.yaml)
2. **Consumer validates requests/responses** against spec
3. **Provider generates responses** per spec
4. **Automated testing tools** (Dredd, Prism, Schemathesis)
5. **Single source of truth** for contract

### Key Benefits
- Single source of truth (OpenAPI spec)
- Includes API documentation
- Multi-consumer support (same spec)
- Built-in schema validation
- Tools ecosystem (mock servers, code generation)
- Supports API versioning

### Example: OpenAPI Spec
```yaml
openapi: 3.0.0
info:
  title: Users API
  version: 1.0.0

paths:
  /users:
    get:
      parameters:
        - name: apiVersion
          in: query
          schema:
            type: string
            enum: ["1", "2"]
      responses:
        "200":
          description: Users list
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/UsersResponse"

components:
  schemas:
    UsersResponse:
      type: object
      required:
        - users
      properties:
        users:
          type: array
          items:
            $ref: "#/components/schemas/User"
    
    User:
      type: object
      required:
        - id
        - name
      properties:
        id:
          type: integer
        name:
          type: string
```

---

## Slide 11: OpenAPI Validation - Consumer & Provider

### Consumer-Side Validation
```javascript
// Validate request before sending
const valid = await validator.ValidateRequest(
  'GET', 
  '/users',
  { apiVersion: '1' }
);

// Make API call
const response = await fetch('/users?apiVersion=1');

// Validate response against spec
const responseValid = await validator.ValidateResponse(
  'GET',
  '/users',
  200,
  response.json()
);
```

### Provider-Side Validation
```javascript
// Middleware to validate requests/responses
app.use(openApiValidationMiddleware);

app.get('/users', (req, res) => {
  // Provider validates request against spec
  // Generate response per spec
  // Validate response before sending
  res.json({ users: [...] });
});
```

### Automated Testing with Dredd
```bash
dredd openapi.yaml http://localhost:5001
# Tests: 16 passing
```

---

## Slide 10: Side-by-Side Comparison

| Feature | Pact | Postman | OpenAPI |
|---------|------|---------|---------|
| **Setup Complexity** | Medium-High | Low | Medium |
| **Learning Curve** | Steep | Gentle | Medium |
| **Consumer Independence** | ✅ Mock server | ❌ Needs provider | ✅ Prism mock |
| **Provider States** | ✅ Built-in | ❌ Manual setup | ❌ Manual setup |
| **Contract Generation** | ✅ Automatic | ❌ Manual | ❌ Manual |
| **Version Management** | ✅ Pact Broker | ⚠️  Manual/Git | ✅ In spec |
| **CI/CD Integration** | ✅ Excellent | ✅ Excellent | ✅ Excellent |
| **Language Support** | ✅ Multi-language | ✅ Any HTTP API | ✅ Any HTTP API |
| **API Documentation** | ❌ No | ⚠️  Limited | ✅ Built-in |
| **Debugging** | ⚠️  CLI/Logs | ✅ Visual UI | ⚠️  CLI/Visual |
| **Team Familiarity** | ❌ Specialized | ✅ Common tool | ⚠️  Dev-friendly |
| **Open Source** | ✅ Fully Open Source | ⚠️  Freemium (OSS core) | ✅ Fully Open Source |

---

## Slide 11: When to Use Pact

### ✅ Ideal Scenarios
- **Microservices architecture** with many service dependencies
- **Multiple consumer teams** consuming same provider
- **Parallel development** - consumer/provider built simultaneously
- **Frequent API changes** requiring version compatibility
- **Strong contract governance** needed across teams
- **Mature DevOps practices** with CI/CD automation

### 🎯 Real-World Use Cases
- E-commerce platform with 20+ microservices
- Mobile app + backend API development
- Third-party API integrations
- Platform-as-a-Service offerings

---

## Slide 12: When to Use Postman

### ✅ Ideal Scenarios
- **Simpler architectures** with fewer dependencies
- **API-first development** with OpenAPI specs
- **Quick start needed** - team needs results fast
- **Limited technical resources** for setup
- **Existing Postman adoption** in the organization
- **Combined testing needs** (functional + contract + performance)

### 🎯 Real-World Use Cases
- Monolith transitioning to microservices
- External partner API validation
- QA team-led testing initiatives
- Smaller teams (2-5 services)

---

## Slide 14: Specmatic - Specification-Driven Testing with Auto-Generation

### Approach
> "Automatic API contract testing from OpenAPI specifications"

### How It Works
1. **Define OpenAPI specification** (specmatic.yaml)
2. **Specmatic auto-generates test scenarios** from spec
3. **Tests run against both consumer and provider**
4. **Mock server automatically generated** from spec
5. **Comprehensive reports generated** automatically

### Key Differentiator from OpenAPI
- Not just validation against spec
- **Automatic test case generation** from spec examples
- Built-in **mock server** (Prism-like)
- **No manual test writing** required
- Generates test scenarios for edge cases

### Key Benefits
- Zero manual test writing
- Mock server for development
- Multi-language support
- Automatic documentation
- Built-in CI/CD support
- Contract verification reports

### Example Test Generated from Spec
```yaml
# From spec: GET /api/products/{id}
# Specmatic automatically generates:
- Test 1: GET /api/products/1 → expects 200 + Product schema
- Test 2: GET /api/products/999 → expects 404 + ErrorResponse
- Test 3: Invalid ID type → validation error
- Test 4: Boundary values → tested automatically
```

---

## Slide 15: Specmatic Example Implementation

### OpenAPI Spec (Source of Truth)
```yaml
openapi: 3.0.0
info:
  title: Product Service
  version: 1.0.0

paths:
  /api/products:
    get:
      responses:
        '200':
          description: All products
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/Product'
    
    post:
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              required: [name, price]
              properties:
                name:
                  type: string
                price:
                  type: number
                  minimum: 0
      responses:
        '201':
          description: Product created
        '400':
          description: Invalid request

components:
  schemas:
    Product:
      type: object
      required: [id, name, price]
      properties:
        id:
          type: integer
        name:
          type: string
        price:
          type: number
```

### What Specmatic Auto-Generates
```
From this single spec, Specmatic generates:

1. Provider Tests:
   ✓ GET /api/products → 200 with array
   ✓ POST /api/products (valid) → 201
   ✓ POST /api/products (invalid) → 400
   ✓ POST with negative price → 400
   ✓ Required fields validation

2. Mock Server:
   GET /api/products → [sample product array]
   POST /api/products → sample 201 response
   
3. Consumer Tests:
   ✓ Handles 200 response
   ✓ Parses Product schema
   ✓ Handles 400 errors
   
4. Documentation:
   HTML API docs from spec
```

---

## Slide 16: Specmatic vs OpenAPI (In Detail)

### Manual OpenAPI Validation
```javascript
// Developer writes validation tests
it('should return 200', async () => {
  const response = await fetch('/api/products');
  expect(response.status).toBe(200);
});

// Must write many tests manually
// Tests don't cover all edge cases
// Test maintenance burden
```

### Specmatic Auto-Generation
```yaml
# Specmatic reads spec and generates all tests automatically
# No test writing needed - tests generated from spec definition

specmatic test --spec products-api.yaml
# Output: 20+ tests run automatically
# Covers happy path, error cases, edge cases
```

### Key Differences

| Aspect | Manual OpenAPI | Specmatic |
|--------|---|---|
| **Test Writing** | Manual (tedious) | Automatic ✅ |
| **Edge Cases** | Developer must think of them | Auto-generated ✅ |
| **Maintenance** | High burden | Low - spec is source ✅ |
| **Mock Server** | Separate tool needed | Built-in ✅ |
| **Documentation** | Separate | From spec ✅ |
| **Completeness** | Gaps likely | Comprehensive ✅ |

---

## Slide 17: Specmatic Architecture

### Provider-Side Testing
```
1. Specmatic reads OpenAPI spec
2. Generates test scenarios (happy + error paths)
3. Sends requests to provider
4. Validates responses match spec
5. Reports coverage
```

### Consumer-Side Testing
```
1. Specmatic generates mock server from spec
2. Consumer tests against mock server
3. Specmatic validates consumer requests match spec
4. Validates consumer handles responses correctly
```

### Workflow Diagram
```
┌─────────────────────────┐
│  OpenAPI Specification  │
│  (Single Source)        │
└────────────┬────────────┘
             │
    ┌────────┴────────┐
    ▼                 ▼
┌──────────┐    ┌──────────┐
│ Provider │    │  Mock    │
│ Tests    │    │ Server   │
│(Auto-gen)│    │(Auto-gen)│
└──────────┘    └──────────┘
    │                │
    ▼                ▼
┌─────────────────────────┐
│  Coverage Report        │
│  - All paths tested     │
│  - Error cases handled  │
│  - Schema validation    │
└─────────────────────────┘
```

---

## Slide 18: When to Use Specmatic

### ✅ Ideal Scenarios
- **API-first development** with OpenAPI specs
- **Multiple consumer teams** consuming same API
- **Want automatic testing** (no manual test writing)
- **Need mock servers** for parallel development
- **Comprehensive API documentation** important
- **Strong governance** and contract enforcement
- **REST APIs** with complex schemas

### 🎯 Real-World Use Cases
- Public APIs with SDKs for multiple languages
- Microservices with strict contract requirements
- Developer experiences (DX) paramount
- API versioning and multi-version support
- Documentation as primary deliverable
- Zero-downtime deployments

### Use Case Example: SaaS API
```
SaaS Provider
  ├─ OpenAPI spec (source of truth)
  ├─ Auto-generated tests
  ├─ Mock server for SDK developers
  ├─ Auto-generated documentation
  └─ Client SDKs (auto-generated from spec)

Client 1 (JavaScript)
  └─ Generated SDK + auto tests

Client 2 (Python)
  └─ Generated SDK + auto tests

Client 3 (Go)
  └─ Generated SDK + auto tests
```

All clients validate against same spec ✅

---

## Slide 19: Specmatic Workshop - What We Built

### Repository Structure
```
specmatic-contract-testing/
├── specs/
│   └── products-api.yaml          # OpenAPI 3.0 spec (source of truth)
│
├── provider/
│   ├── src/
│   │   └── index.js               # Express.js API server
│   ├── Dockerfile
│   └── package.json
│
├── consumer/
│   ├── src/
│   │   ├── api-client.js          # API client implementation
│   │   └── contract.test.js       # Jest contract tests
│   ├── jest.config.js
│   └── package.json
│
├── specmatic.yaml                 # Specmatic config
├── docker-compose.yml
│
└── Documentation
    ├── README.md                  # Complete guide
    ├── QUICK-START.md             # 5-minute setup
    ├── ARCHITECTURE.md            # Concepts
    ├── SETUP.md                   # Installation
    ├── EXAMPLES.md                # Code examples
    └── INDEX.md                   # Navigation
```

### What Tests Are Generated

**Provider Tests** (Auto-generated by Specmatic):
```
✓ GET /api/products → 200 + array
✓ POST /api/products (valid) → 201 + product
✓ POST /api/products (invalid name) → 400
✓ POST /api/products (negative price) → 400
✓ GET /api/products/{id} → 200 + product
✓ GET /api/products/999 → 404 + error
✓ PUT /api/products/{id} → 200
✓ DELETE /api/products/{id} → 204
+ Edge cases, boundary conditions, type validation
```

**Consumer Tests** (Against generated mock):
```
✓ Can get all products
✓ Can parse product schema
✓ Can handle 404 errors
✓ Can create new product
✓ Can update existing product
✓ Can delete product
+ All scenarios from spec
```

---

## Slide 20: Comparison Table - All Four Approaches

| Feature | Pact | Postman | OpenAPI | Specmatic |
|---------|------|---------|---------|-----------|
| **Approach** | Consumer-driven | Collection-based | Spec-driven | Spec-driven + Auto |
| **Test Generation** | Manual | Manual | Manual | ✅ Automatic |
| **Mock Server** | ✅ Yes | ⚠️ Limited | ✅ Yes | ✅ Yes |
| **Auto Test Gen** | ❌ No | ❌ No | ❌ No | ✅ Yes |
| **Multi-Consumer** | ⚠️ Per pair | ⚠️ Shared | ✅ Single spec | ✅ Single spec |
| **Documentation** | ❌ Separate | ⚠️ Limited | ✅ In spec | ✅ In spec |
| **Setup Time** | High | Low | Medium | Medium |
| **Learning Curve** | Steep | Low | Medium | Medium |
| **Spec Required** | ❌ No | ❌ No | ✅ Yes | ✅ Yes |
| **CI/CD Ready** | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| **Language Support** | ✅ Multi | ✅ Any API | ✅ Any API | ✅ Any API |
| **Maintenance** | Medium | High | Low | ✅ Lowest |
| **Code Generation** | ❌ Limited | ❌ No | ⚠️ External | ✅ Built-in |

---

## Slide 21: Decision Framework - When to Use Each

### Use Pact If:
✅ Multiple service teams (microservices)  
✅ Strong DevOps culture with automation  
✅ Consumer independence critical  
✅ Complex provider state management needed  
✅ Already invested in Pact ecosystem  

### Use Postman If:
✅ Quick start needed (< 1 day)  
✅ Team already knows Postman  
✅ Exploratory testing priority  
✅ Mixed functional + contract testing  
✅ Limited technical resources  

### Use OpenAPI If:
✅ Spec exists or being created  
✅ Need comprehensive documentation  
✅ Multiple consumer versions  
✅ Prefer specification as contract  
✅ Mock servers essential  

### Use Specmatic If:
✅ Spec-first development  
✅ Don't want to write tests manually  
✅ Need automatic test generation  
✅ Mock server is important  
✅ Documentation is deliverable  
✅ Multi-language SDKs needed  

### Hybrid Approach (Recommended)
```
Layer 1: Specmatic
├─ OpenAPI spec (source of truth)
├─ Auto-generated tests
├─ Mock servers for development
└─ Auto-generated documentation

Layer 2: Pact (for critical pairs)
├─ Consumer-provider verification
├─ Version compatibility
└─ Deployment readiness

Layer 3: Postman (for exploratory)
├─ Functional testing
├─ Performance testing
└─ Manual validation

Result: Comprehensive API contract coverage
```

---

## Slide 14: Pact Workshop - What We Built

### Repository Structure
```
pact-workshop-dotnet/
├── Consumer/
│   ├── src/ApiClient.cs          # HTTP client
│   └── tests/ApiTest.cs          # Pact consumer tests
├── Provider/
│   ├── src/ProductsController.cs # API implementation
│   └── tests/
│       ├── ProductTest.cs        # Pact verification
│       └── Middleware/
│           ├── ProviderStateMiddleware.cs
│           └── AuthTokenRequestFilter.cs
└── pacts/
    └── ApiClient-ProductService.json  # Generated contract
```

### Key Learnings
- Consumer defines expectations in code
- Provider states manage test data
- Request filters handle dynamic data (auth tokens)
- Port conflicts can break tests
- Native libraries must be copied

---

## Slide 15: OpenAPI Demo - What We Built

### Repository Structure
```
openapi-contract-testing/
├── openapi.yaml              # OpenAPI 3.0 spec (single source of truth)
├── docker-compose.yml        # Docker Compose setup
│
├── provider/                 # Node.js Provider API
│   ├── index.js             # Express server (v1 & v2 support)
│   ├── Dockerfile
│   └── package.json
│
└── consumer/                # Jest Consumer Tests
    ├── v1-client.js         # SF 17.1 client
    ├── v2-client.js         # SF 18.1 client
    ├── schema-validator.js  # JSON Schema validator
    └── tests/
        ├── v1.test.js       # 7 v1 tests (SF 17.1)
        └── v2.test.js       # 9 v2 tests (SF 18.1)
```

### Real-World Scenario
- **SF 17.1 Consumer**: Calls v1 endpoint (legacy)
- **SF 18.1 Consumer**: Calls v2 endpoint (new)
- **Provider**: Supports both simultaneously
- **Contract**: Single OpenAPI spec validates all

### Test Results
- **Consumer Tests**: 16 passing ✅
- **All scenarios covered**: Happy path, versioning, schema validation

---

## Slide 15: Architecture Patterns

### Pact: Consumer-Driven Workflow
```
┌─────────────────────────────────────────────────┐
│           CONSUMER TEAM                         │
│  1. Write feature needing API                   │
│  2. Define contract in Pact test                │
│  3. Push contract to Pact Broker                │
│  4. Develop against mock server                 │
└────────────────┬────────────────────────────────┘
                 │ Contract
                 ▼
┌─────────────────────────────────────────────────┐
│           PROVIDER TEAM                         │
│  5. Pull contract from Pact Broker              │
│  6. Run provider verification tests             │
│  7. If fail: implement changes                  │
│  8. Publish verification results                │
└─────────────────────────────────────────────────┘
```

### Postman: API-First Workflow
```
┌─────────────────────────────────────────────────┐
│           API DESIGN PHASE                      │
│  1. Define OpenAPI/Swagger spec                 │
│  2. Generate Postman collection                 │
│  3. Add contract test assertions                │
│  4. Share collection with teams                 │
└────────────────┬────────────────────────────────┘
                 │ Collection
         ┌───────┴────────┐
         ▼                ▼
┌──────────────┐   ┌──────────────┐
│   PROVIDER   │   │   CONSUMER   │
│ Implement    │   │ Implement    │
│ Run tests    │   │ Run tests    │
└──────────────┘   └──────────────┘
```

### OpenAPI: Specification-First Workflow
```
┌─────────────────────────────────────────────────┐
│      API SPECIFICATION PHASE                    │
│  1. Design OpenAPI spec (yaml/json)             │
│  2. Validate spec syntax & semantics            │
│  3. Generate mock server (Prism)                │
│  4. Distribute to all teams                     │
└────────────────┬────────────────────────────────┘
                 │ OpenAPI Spec
         ┌───────┴────────┐
         ▼                ▼
┌──────────────┐   ┌──────────────┐
│   PROVIDER   │   │   CONSUMER   │
│ Validate req │   │ Validate req │
│ Validate res │   │ Validate res │
│ Impl by spec │   │ Test by spec │
└──────────────┘   └──────────────┘
```

---

## Slide 16: CI/CD Integration Examples

### Pact in Azure Pipelines
```yaml
# Consumer pipeline
- task: DotNetCoreCLI@2
  inputs:
    command: 'test'
    projects: '**/Consumer.Tests.csproj'
  displayName: 'Run Consumer Pact Tests'

- task: PublishPact@1
  inputs:
    pactFilePath: 'pacts/*.json'
    pactBrokerUrl: $(PACT_BROKER_URL)
    version: $(Build.BuildNumber)
```

### Postman/Newman in Azure Pipelines
```yaml
# API contract tests
- task: Npm@1
  inputs:
    command: 'custom'
    customCommand: 'install -g newman'

- script: |
    newman run contract-tests.json \
      -e environment.json \
      --reporters cli,junit \
      --reporter-junit-export results.xml
  displayName: 'Run Contract Tests'
```

### OpenAPI Validation in Azure Pipelines
```yaml
# OpenAPI contract validation
- task: Npm@1
  inputs:
    command: 'custom'
    customCommand: 'install -g swagger-cli @stoplight/spectral-cli'

- script: |
    swagger-cli validate openapi.yaml
    spectral lint openapi.yaml
  displayName: 'Validate OpenAPI Spec'

- script: |
    npm install -g dredd
    dredd openapi.yaml http://localhost:5001
  displayName: 'Run Dredd Contract Tests'
```

---

## Slide 17: Cost-Benefit Analysis

### Pact
**Upfront Costs**:
- Learning curve: 2-4 weeks per team
- Infrastructure: Pact Broker setup
- Integration: CI/CD pipeline changes
- Maintenance: Keep provider states updated

**Long-term Benefits**:
- Parallel development unlocked
- 70-90% reduction in integration bugs
- Faster feedback loops (minutes vs hours)
- Self-service testing for consumers

**ROI Timeline**: 2-3 months for teams with 5+ services

### Postman
**Upfront Costs**:
- Collection creation: 1-2 days
- Test script writing: Ongoing
- CI/CD integration: 1 day

**Long-term Benefits**:
- Quick validation of API changes
- Familiar tooling reduces friction
- Combined with functional testing

**ROI Timeline**: Immediate for existing Postman users

---

## Slide 18: Migration Path

### From Postman → Pact
**When to Consider**:
- Growing microservices (>5 services)
- Frequent breaking changes
- Need for parallel development
- Multiple consumer teams

**Migration Steps**:
1. Keep Postman for exploratory testing
2. Add Pact for critical service pairs
3. Set up Pact Broker
4. Train teams incrementally
5. Gradually expand Pact coverage

### From Pact → Postman
**When to Consider**:
- Simplifying testing strategy
- Reducing tooling overhead
- Team struggling with Pact complexity

**Migration Steps**:
1. Export Pact contracts as reference
2. Create Postman collections from contracts
3. Add contract validation scripts
4. Maintain both temporarily
5. Phase out Pact infrastructure

---

## Slide 19: Best Practices

### Pact Best Practices
✅ **Do**:
- Use provider states for data setup
- Keep contracts focused (one endpoint per test)
- Version contracts in Pact Broker
- Implement request filters for dynamic data
- Run consumer tests before pushing
- Use matchers (type, regex) not exact values

❌ **Don't**:
- Test business logic in contract tests
- Include authentication in contracts (filter it)
- Commit pact files to provider repo
- Hardcode timestamps or random values
- Make contracts too strict

### Postman Best Practices
✅ **Do**:
- Version collections in Git
- Use environment variables for endpoints
- Write atomic, focused tests
- Include schema validation
- Run in CI/CD pipeline
- Document expected behaviors

❌ **Don't**:
- Test only happy paths
- Hardcode data values
- Skip response structure validation
- Ignore error scenarios
- Run only manually

---

## Slide 20: Tooling Ecosystem

### Pact Ecosystem
- **Pact Broker**: Contract storage & verification results
- **Pact Foundation**: Multi-language implementations
- **Can I Deploy**: Compatibility checking tool
- **Webhooks**: Trigger provider tests on contract publish
- **Pact Plugins**: Extend functionality (gRPC, GraphQL)

**OSS vs Commercial**:
- Open Source: Self-hosted Pact Broker
- PactFlow: SaaS offering with advanced features

### Postman Ecosystem
- **Postman**: Desktop & web application
- **Newman**: CLI runner for automation
- **Postman Cloud**: Team collaboration
- **Postman Monitors**: Scheduled test runs
- **Postman Flows**: Visual API workflows

**Pricing**: Free for individuals, paid for teams

---

## Slide 21: Real-World Success Stories

### Pact Adoption
**Company**: SEEK (Australian job board)
- **Challenge**: 200+ microservices, integration test nightmare
- **Solution**: Pact across all services
- **Results**: 
  - 95% reduction in integration bugs in production
  - Deploy confidence increased dramatically
  - Development velocity improved 40%

**Company**: REA Group (Real estate platform)
- **Challenge**: Mobile apps + backend API coordination
- **Solution**: Pact between mobile & API teams
- **Results**:
  - Parallel development enabled
  - Breaking changes caught before release
  - Release cycle shortened from weeks to days

### Postman Adoption
**Typical Use Case**: 
- Mid-size companies (50-200 engineers)
- Transitioning from manual to automated testing
- Quick wins with existing Postman knowledge
- Combined functional + contract testing

---

## Slide 22: Decision Framework

### Choose Pact If:
✅ You have >5 microservices  
✅ Multiple teams consume same APIs  
✅ Frequent API changes cause issues  
✅ You need parallel development  
✅ Strong DevOps/CI culture  
✅ Budget for learning curve

### Choose Postman If:
✅ <5 services or simpler architecture  
✅ Quick start is priority  
✅ Team already uses Postman  
✅ Combined testing approach desired  
✅ Limited technical resources  
✅ Provider-driven development

### Choose OpenAPI If:
✅ API-first development already in place  
✅ Need built-in API documentation  
✅ Want mock servers (Prism)  
✅ Multiple consumer versions (v1/v2)  
✅ Need spec-driven governance  
✅ Prefer specification over code

### Choose Specmatic If:
✅ Spec-first development with OpenAPI  
✅ Don't want to manually write tests  
✅ Need automatic test generation  
✅ Mock servers are essential  
✅ Documentation is a deliverable  
✅ Multi-language SDK support  
✅ Comprehensive coverage important

### Use All Four?
🤔 **Multi-Layer Strategy**:
- **Specmatic**: Automatic spec coverage
- **Pact**: Critical service contracts
- **Postman**: Exploratory/functional testing
- **OpenAPI**: Source of truth & documentation
- **Each tool excels** at different aspects

---

## Slide 23: Implementation Roadmap

### Phase 1: Foundation (Week 1-2)
- [ ] Choose tool(s) based on decision framework
- [ ] Set up infrastructure (mocks, CI/CD)
- [ ] Train pilot team
- [ ] Select 1-2 service pairs for POC

### Phase 2: Pilot (Week 3-6)
- [ ] Implement contracts for pilot services
- [ ] Integrate with CI/CD pipeline
- [ ] Document learnings & patterns
- [ ] Measure impact (bugs caught, time saved)

### Phase 3: Scale (Month 2-3)
- [ ] Roll out to additional teams
- [ ] Establish governance & best practices
- [ ] Create templates & examples
- [ ] Monitor adoption & support teams

### Phase 4: Optimize (Month 3+)
- [ ] Refine workflow based on feedback
- [ ] Automate more scenarios
- [ ] Track metrics (contract coverage, bugs prevented)
- [ ] Continuous improvement

### Specmatic-Specific Timeline
- **Day 1**: Write OpenAPI spec
- **Day 2**: Run Specmatic tests (auto-generated)
- **Day 3**: Generate mock server
- **Day 4**: Distribute to consumers
- **Day 5**: Integrate into CI/CD

---

## Slide 24: Key Takeaways

### Contract Testing Is Essential
1. **Bridges the gap** between unit and integration tests
2. **Enables parallel development** across teams
3. **Catches issues early** before expensive integration failures
4. **Reduces test brittleness** compared to E2E tests

### Tool Selection Matters
- **Pact**: Best for mature microservices architectures
- **Postman**: Best for simpler setups or quick wins
- **OpenAPI**: Best for spec-first and documentation
- **Specmatic**: Best for automatic testing and mock servers
- **Context is key**: No one-size-fits-all solution

### Success Factors
✅ Team buy-in and training  
✅ CI/CD integration  
✅ Clear governance  
✅ Start small, scale gradually  
✅ Choose right tool for context

### The Future: Multi-Tool Approach
Expect to use multiple tools together:
```
Specmatic (Auto tests) + Pact (Contracts) + 
Postman (Exploratory) + OpenAPI (Spec)
= Comprehensive API Quality
```

---

## Slide 25: Resources & Next Steps

### Pact Resources
- **Docs**: https://docs.pact.io
- **Workshop**: This repository (pact-workshop-dotnet)
- **Community**: Slack, GitHub discussions
- **PactFlow**: https://pactflow.io (commercial offering)

### Postman Resources
- **Docs**: https://learning.postman.com
- **Newman**: https://www.npmjs.com/package/newman
- **Example**: `postman-contract-testing/` directory
- **Community**: Postman community forums

### OpenAPI Resources
- **Docs**: https://spec.openapis.org
- **Prism Mock Server**: https://stoplight.io/prism/
- **Dredd**: https://dredd.org/
- **Spectral Linter**: https://meta.stoplight.io/docs/spectral
- **Example**: `openapi-contract-testing/` directory

### Specmatic Resources
- **Docs**: https://specmatic.io
- **GitHub**: https://github.com/znsio/specmatic
- **Community**: GitHub discussions
- **Example**: `specmatic-contract-testing/` directory (NEW!)

### Getting Started
1. Review all four examples in this repository
2. Run the demos locally (Pact, Postman, OpenAPI, Specmatic)
3. Choose primary approach for your team
4. Consider hybrid approach as you scale
5. Start with one service pair
6. Iterate and expand coverage

---

## Slide 26: Q&A Discussion Topics

**Common Questions**:

1. **"Do we need Pact Broker or can we use Git?"**
   - Answer: Git works for small teams, Broker enables scale

2. **"Can Specmatic auto-generate all my tests?"**
   - Answer: Yes - from spec definition automatically

3. **"What about GraphQL/gRPC?"**
   - Answer: Pact and Postman support these (with plugins/setup)

4. **"How do we version contracts?"**
   - Answer: Pact Broker, Git/tagging, or spec versioning

5. **"What about testing third-party APIs we don't control?"**
   - Answer: Postman better suited; Pact designed for internal APIs

6. **"Should we use all four tools?"**
   - Answer: Hybrid approach recommended - each has strengths

---

## Slide 27: Demo Time!

### Live Demonstrations

**Demo 1: Pact Workflow** (10 minutes)
1. Show consumer test writing
2. Run test, generate pact file
3. Show pact file structure
4. Run provider verification
5. Introduce breaking change & show failure

**Demo 2: Postman Workflow** (5 minutes)
1. Import collection
2. Run tests in Postman UI
3. Show Newman CLI execution
4. Review test results

**Demo 3: Specmatic Workflow** (5 minutes)
1. Show OpenAPI spec
2. Run Specmatic tests (auto-generated)
3. Generate mock server
4. Show generated documentation

**Demo 4: Debugging** (5 minutes)
- Show how to troubleshoot common issues
- Port conflicts, missing headers, schema validation, etc.

---

## Slide 28: Thank You!

### Contact & Feedback
- **Questions?** Happy to discuss offline
- **Want to try it?** Clone the repository and follow the README
- **Need help?** Reach out for implementation guidance

### Repository Contents
```
✅ Working Pact example (Consumer + Provider)
✅ Working Postman example (Consumer + Provider)  
✅ Working OpenAPI example (Consumer + Provider + Spec)
✅ Working Specmatic example (Consumer + Provider + Auto Tests) ⭐ NEW
✅ Troubleshooting documentation
✅ Comparison matrices
✅ This presentation
```

**Clone it**: 
```bash
git clone <your-repo-url>
cd contract-testing
```

### Four Approaches, One Goal
**Pact** + **Postman** + **OpenAPI** + **Specmatic**
= Comprehensive API Contract Coverage

**Start small, scale gradually, choose your tools wisely!**

---

## Additional Slides (Backup)

### Backup Slide 1: Detailed Specmatic Flow Diagram
[Insert detailed sequence diagram showing auto test generation flow]

### Backup Slide 2: Cost Analysis Spreadsheet
[Insert TCO comparison table with all four tools]

### Backup Slide 3: Alternative Tools
- Spring Cloud Contract (Java ecosystem)
- Schemathesis (GraphQL/REST auto testing)
- VCR (Record & replay)
- WireMock (Mock server)

### Backup Slide 4: Specmatic vs Others in Detail
[Insert detailed comparison matrix with 20+ dimensions]

---

**END OF PRESENTATION**
