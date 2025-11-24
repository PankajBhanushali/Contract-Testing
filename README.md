# Contract Testing: Pact & Postman Implementation

A comprehensive guide demonstrating two approaches to contract testing in .NET microservices: **Pact** (bi-directional contract testing) and **Postman** (API contract validation).

## 📋 Table of Contents

1. [Overview](#overview)
2. [Project Structure](#project-structure)
3. [Pact Contract Testing](#pact-contract-testing)
4. [Postman Contract Testing](#postman-contract-testing)
5. [Pact Broker Integration](#pact-broker-integration)
6. [Getting Started](#getting-started)
7. [Running Tests](#running-tests)
8. [Key Features](#key-features)

---

## Overview

**Contract Testing** ensures that the agreements (contracts) between independent services are met. This project implements two popular approaches:

### Pact
- **Bi-directional contract testing**: Contracts are generated from consumer expectations and verified against provider implementations
- **Best for**: Microservices with clear provider-consumer relationships
- **Framework**: PactNet 5.x with xUnit

### Postman
- **API-level contract testing**: Tests verify that APIs conform to expected contract behavior
- **Best for**: Documenting and validating API specifications
- **Tool**: Newman CLI for automated execution

---

## Project Structure

```
Contract-Testing/
├── pact-contract-testing/          # Pact implementation
│   ├── Consumer/
│   │   ├── src/
│   │   │   └── ApiClient.cs        # HTTP client with Bearer token support
│   │   └── tests/
│   │       └── ApiTest.cs          # 5 comprehensive Pact tests
│   ├── Provider/
│   │   ├── src/
│   │   │   ├── Program.cs          # ASP.NET Core host
│   │   │   ├── Controllers/
│   │   │   │   └── ProductsController.cs
│   │   │   ├── Middleware/
│   │   │   │   └── AuthorizationMiddleware.cs  # Bearer token validation
│   │   │   ├── Model/
│   │   │   │   └── Product.cs
│   │   │   └── Repositories/
│   │   │       ├── IProductRepository.cs
│   │   │       └── ProductRepository.cs
│   │   └── tests/
│   │       ├── ProductTest.cs
│   │       ├── TestStartup.cs
│   │       └── Middleware/
│   │           ├── ProviderStateMiddleware.cs   # Dynamic test state management
│   │           └── AuthTokenRequestFilter.cs    # Token refresh for testing
│   └── pacts/
│       └── ApiClient-ProductService.json  # Generated contract file
│
├── postman-contract-testing/       # Postman implementation
│   ├── Consumer/
│   │   ├── src/
│   │   │   ├── ApiClient.cs
│   │   │   ├── Program.cs
│   │   │   └── consumer.csproj
│   │   └── tests/
│   ├── Provider/
│   │   ├── src/
│   │   │   ├── Program.cs          # Minimal ASP.NET Core API
│   │   │   ├── Controllers/
│   │   │   │   └── ProductsController.cs
│   │   │   ├── appsettings.json
│   │   │   └── provider.csproj
│   │   └── tests/
│   └── postman-collections/
│       ├── Product-API-Contract-Tests.postman_collection.json  # 3 contract tests
│       └── Product-API-Environment.postman_environment.json    # baseUrl config
│
├── step5/ through step10/          # Progressive step-by-step examples
│
├── PACT-BROKER-GUIDE.md            # Comprehensive 9-part Pact Broker guide
├── POSTMAN-ANALYSIS.md             # Detailed Postman implementation analysis
├── CONTRACT-TESTING-QUICK-REFERENCE.md
├── LEARNING.md                     # Learning notes and resources
├── LICENSE
└── README.md                       # This file
```

---

## Pact Contract Testing

### What is Pact?

Pact enables **consumer-driven contract testing**, where consumers define the API they expect and providers verify they implement it. Contracts are published and can be verified against multiple versions.

### Key Components

#### Consumer (ApiClient)
- **Purpose**: Defines expectations for the Product Service API
- **Location**: `pact-contract-testing/Consumer/`
- **Key File**: `ApiClient.cs` - HTTP client with optional Bearer token support
- **Test Coverage**: 5 scenarios
  1. Get all products (happy path)
  2. Get all products (empty results)
  3. Get product by ID
  4. Product not found (404)
  5. Authorization failure (401)

#### Provider (ProductService)
- **Purpose**: Implements the Product Service API
- **Location**: `pact-contract-testing/Provider/`
- **Key Files**:
  - `Program.cs` - ASP.NET Core configuration
  - `ProductsController.cs` - REST endpoints
  - `AuthorizationMiddleware.cs` - Bearer token validation

### Authorization Implementation

Bearer tokens with **1-hour expiration window** using ISO timestamp format:

```csharp
// Token format: "Bearer yyyy-MM-ddTHH:mm:ss.fffZ"
Authorization: Bearer 2025-11-24T14:30:00.000Z
```

### Provider States

Dynamic test scenarios managed via `ProviderStateMiddleware.cs`:

| State | Behavior |
|-------|----------|
| `products exist` | Returns 2 products (IDs: 9, 10) |
| `no products exist` | Returns empty array |
| `product 10 exists` | GET /products/10 succeeds |
| `product 11 doesn't exist` | GET /products/11 returns 404 |
| `no auth token` | Authorization header is omitted |

### Test Results

```
Consumer Tests: 5/5 PASSED ✅
Provider Verification: All 5 interactions verified ✅
```

---

## Postman Contract Testing

### What is Postman?

Postman is an API testing platform that enables contract testing through collections with JavaScript-based assertions. **Newman CLI** automates collection execution.

### Key Components

#### API Collection
- **Location**: `postman-contract-testing/postman-collections/`
- **File**: `Product-API-Contract-Tests.postman_collection.json`
- **Tests**: 3 contract tests with JavaScript assertions

#### Test Scenarios

1. **Get All Products**
   - Endpoint: `GET /api/products`
   - Assertions: 5 (Status 200, response type, array structure, product schema)

2. **Get Product by ID**
   - Endpoint: `GET /api/products/{id}`
   - Assertions: 4 (Status 200, response type, product ID, properties)

3. **Not Found (404)**
   - Endpoint: `GET /api/products/99`
   - Assertions: 2 (Status 404, response body)

#### Provider API
- **Location**: `postman-contract-testing/Provider/`
- **Framework**: Minimal ASP.NET Core API
- **Port**: `localhost:5001`
- **Data**: 2 hardcoded products

### Test Results

```
Newman Execution: 11/12 assertions PASSED ✅
Minor Warning: 404 response body size
```

---

## Pact Broker Integration

### Overview

**Pact Broker** is a repository for managing pacts. It enables:
- Centralized pact storage
- Verifying compatibility between consumer and provider versions
- CI/CD integration

### Docker Setup

```bash
docker run -d \
  --name pact-broker \
  -e PACT_BROKER_DATABASE_USERNAME=admin \
  -e PACT_BROKER_DATABASE_PASSWORD=password \
  -e PACT_BROKER_DATABASE_URL=sqlite:////tmp/pact_broker.db \
  -p 9292:9292 \
  pactfoundation/pact-broker:latest
```

### Publishing Pacts

Pacts are published via HTTP API:

```bash
PUT http://localhost:9292/pacts/provider/ProductService/consumer/ApiClient/version/1.0.0
Content-Type: application/json

{
  "interactions": [
    // Pact interactions
  ]
}
```

### Broker UI

Access the Pact Broker dashboard at: **http://localhost:9292**

Features:
- View published pacts
- Check consumer-provider compatibility
- Verify pact interactions
- Integration with CI/CD pipelines

---

## Getting Started

### Prerequisites

- **.NET 8.0 SDK** or later
- **Docker** (for Pact Broker)
- **Postman** or **Newman CLI** (for Postman tests)
- **Git** (for version control)

### Installation

1. **Clone the repository**
   ```powershell
   git clone https://github.com/PankajBhanushali/Contract-Testing.git
   cd "Contract Testing"
   ```

2. **Restore dependencies**
   ```powershell
   dotnet restore
   ```

3. **Start Pact Broker (Optional)**
   ```bash
   docker run -d --name pact-broker -p 9292:9292 pactfoundation/pact-broker:latest
   ```

---

## Running Tests

### Pact Consumer Tests

```powershell
cd "pact-contract-testing\Consumer\tests"
dotnet test
```

**Expected Output**:
```
Test Results: 5 passed, 0 failed
```

### Pact Provider Tests

```powershell
cd "pact-contract-testing\Provider"
dotnet test
```

**Verifies**:
- Provider implements all pact interactions
- Authorization middleware validates tokens
- Provider states work correctly

### Postman Contract Tests

#### Option 1: Using Postman UI
1. Import collection: `postman-collections/Product-API-Contract-Tests.postman_collection.json`
2. Import environment: `postman-collections/Product-API-Environment.postman_environment.json`
3. Run collection

#### Option 2: Using Newman CLI

```powershell
# Install Newman (one-time)
npm install -g newman

# Run collection
newman run `
  "postman-contract-testing/postman-collections/Product-API-Contract-Tests.postman_collection.json" `
  -e "postman-contract-testing/postman-collections/Product-API-Environment.postman_environment.json"
```

**Expected Output**:
```
Summary:
│ Total:    12 Assertions
│ Passed:   11
│ Failed:   1 (Minor - 404 body size)
```

### End-to-End Test Flow

1. **Start Provider**
   ```powershell
   cd pact-contract-testing/Provider
   dotnet run
   # Runs on http://localhost:5000
   ```

2. **Run Consumer Tests**
   ```powershell
   cd pact-contract-testing/Consumer/tests
   dotnet test
   ```

3. **Verify with Pact Broker**
   ```powershell
   # Access UI at http://localhost:9292
   # View verified interactions and compatibility matrix
   ```

---

## Key Features

### ✅ Bi-directional Contracts (Pact)
- Consumers define expectations
- Providers verify implementation
- Automated compatibility checking

### ✅ Bearer Token Authorization
- Dynamic token generation with timestamps
- 1-hour expiration window
- ISO 8601 format validation

### ✅ Provider States
- Dynamic test scenario management
- Support for 5+ different test states
- Middleware-based state transitions

### ✅ Token Refresh During Testing
- `AuthTokenRequestFilter.cs` automatically updates tokens
- Ensures tests pass with time-sensitive validation

### ✅ Multiple Contract Approaches
- **Pact**: For microservices with provider-consumer relationships
- **Postman**: For general API contract validation

### ✅ Comprehensive Documentation
- `PACT-BROKER-GUIDE.md`: 9-part comprehensive guide
- `POSTMAN-ANALYSIS.md`: Detailed Postman implementation
- `LEARNING.md`: Learning notes and resources
- Step-by-step examples in `step5/` through `step10/`

### ✅ CI/CD Ready
- GitHub Actions workflow included (`.github/workflows/test.yml`)
- Docker support for Pact Broker
- Automated test execution

---

## Additional Resources

| File | Purpose |
|------|---------|
| `PACT-BROKER-GUIDE.md` | Complete 9-part guide to Pact Broker setup and usage |
| `POSTMAN-ANALYSIS.md` | Detailed analysis of Postman contract testing implementation |
| `CONTRACT-TESTING-QUICK-REFERENCE.md` | Quick reference guide for contract testing concepts |
| `LEARNING.md` | Learning notes and additional resources |

---

## Common Issues & Solutions

### Issue: Token Validation Fails
**Solution**: Ensure the token timestamp is within 1 hour of server time. Use `AuthTokenRequestFilter.cs` to automatically refresh tokens during testing.

### Issue: Pact Broker Connection Fails
**Solution**: Verify Docker is running and Pact Broker is accessible at `http://localhost:9292`.

### Issue: Consumer Tests Hang
**Solution**: Ensure Provider is running on `http://localhost:5000` before running consumer tests.

### Issue: Postman Collections Not Found
**Solution**: Ensure Newman is installed globally: `npm install -g newman`.

---

## Project Highlights

- **5 Pact Tests**: Comprehensive coverage of happy path, error cases, and authorization
- **2 Implementations**: Both Pact and Postman approaches demonstrated
- **Full Authorization**: Bearer token validation with timestamp-based expiration
- **Provider States**: Dynamic test scenario management
- **Docker Integration**: Pact Broker setup with comprehensive guide
- **Documentation**: Step-by-step guides and learning resources

---

## License

See `LICENSE` file for details.

---

## Getting Help

Refer to the detailed guides:
- **Pact Setup**: See `PACT-BROKER-GUIDE.md`
- **Postman Tests**: See `POSTMAN-ANALYSIS.md`
- **Contract Testing Concepts**: See `CONTRACT-TESTING-QUICK-REFERENCE.md`

---

**Last Updated**: November 24, 2025  
**Project URL**: https://github.com/PankajBhanushali/Contract-Testing
