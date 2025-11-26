# Architecture & Design Patterns

## Overview

This document explains the architecture and design patterns used in the Specmatic .NET contract testing example.

## Contract-First Architecture

### Pattern: Specification-Driven Design

```
┌─────────────────────────────────────────────────────────────┐
│                     1. SPECIFICATION                         │
│               OpenAPI 3.0 (products-api.yaml)               │
│                                                              │
│  • Complete API contract                                    │
│  • All endpoints defined                                    │
│  • All schemas specified                                    │
│  • All status codes documented                             │
└──────────────────────────┬──────────────────────────────────┘
                           │
       ┌───────────────────┼───────────────────┐
       │                   │                   │
       ▼                   ▼                   ▼
┌─────────────┐     ┌────────────┐      ┌──────────────┐
│  PROVIDER   │     │   CONSUMER │      │ TESTS        │
│             │     │            │      │              │
│ Implements  │     │ Uses       │      │ Validates    │
│ specification     │ per spec   │      │ compliance   │
│             │     │            │      │              │
│ ASP.NET API │     │ .NET Client│      │ xUnit        │
└─────────────┘     └────────────┘      └──────────────┘
```

### Key Principle: Single Source of Truth

The specification is **the** source of truth:

```
❌ Multiple sources (spec + implementation + tests)
   ├── Spec drifts from code
   ├── Tests don't match spec
   ├── Consumer assumptions diverge
   └── Integration fails

✅ Single source (specification)
   ├── Code implements spec
   ├── Tests validate spec
   ├── Everyone aligns
   └── Integration works
```

## Layered Architecture

### Layer 1: Specification (OpenAPI)

```yaml
# specs/products-api.yaml

openapi: 3.0.0
info:
  title: Product Service API
  version: 1.0.0

paths:
  /api/products:
    get:
      summary: Get all products
      parameters:
        - name: skip
          schema: { type: integer, minimum: 0 }
      responses:
        '200':
          schema:
            type: array
            items: { $ref: '#/components/schemas/Product' }

components:
  schemas:
    Product:
      type: object
      required: [id, name, price, inStock, createdAt]
```

**Responsibilities:**
- Define all endpoints
- Specify request/response schemas
- Document status codes
- Declare validation rules

### Layer 2: Provider (ASP.NET Core)

```csharp
// Provider/src/Program.cs

app.MapGet("/api/products", async (IProductRepository repo, int skip = 0, int take = 10) =>
{
    // 1. Validate request per spec
    if (skip < 0 || take < 1 || take > 100)
        return Results.BadRequest(...);
    
    // 2. Execute business logic
    var products = await repo.GetAllAsync(skip, take);
    
    // 3. Return response per spec
    return Results.Ok(products);
})
.WithName("GetProducts")
.WithOpenApi();
```

**Responsibilities:**
- Implement all endpoints
- Validate requests per spec
- Execute business logic
- Return responses matching spec

**Design Pattern: Repository Pattern**

```csharp
public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllAsync(int skip = 0, int take = 10);
    Task<Product?> GetByIdAsync(int id);
    Task<Product> CreateAsync(CreateProductRequest request);
    Task<Product?> UpdateAsync(int id, UpdateProductRequest request);
    Task<bool> DeleteAsync(int id);
}

public class ProductRepository : IProductRepository
{
    private static readonly List<Product> Products = [];
    
    // In-memory storage for demo
    // In production: would use EF Core + database
}
```

**Benefits:**
- Abstraction from storage
- Testable without database
- Easy to swap implementations
- Clear responsibility

### Layer 3: Consumer (xUnit Tests)

```csharp
// Consumer/tests/ProductApiContractTests.cs

public class ProductApiContractTests
{
    private readonly ProductApiClient _client;
    
    public ProductApiContractTests()
    {
        _client = new ProductApiClient("http://localhost:5000");
    }
    
    [Fact]
    public async Task GetAllProducts_ShouldReturn_ArrayOfProducts()
    {
        // 1. Make request per spec
        var products = await _client.GetAllProductsAsync();
        
        // 2. Validate response per spec
        products.Should().BeOfType<List<Product>>();
        products.Should().NotBeEmpty();
    }
}
```

**Responsibilities:**
- Make valid requests
- Verify responses match spec
- Test happy paths
- Test error cases

## Design Patterns Used

### 1. Repository Pattern

**Problem:** Business logic depends on data access

**Solution:** Abstract data access behind interface

```csharp
// Interface: Product/src/ProductRepository.cs
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id);
    Task<Product> CreateAsync(CreateProductRequest request);
}

// Implementation: In-memory for demo
public class ProductRepository : IProductRepository
{
    private static readonly List<Product> Products = [];
}

// Usage: Program.cs
builder.Services.AddScoped<IProductRepository, ProductRepository>();
```

**Benefits:**
- Easy to test
- Easy to replace
- Supports different storage backends

### 2. Dependency Injection

**Problem:** Components tightly coupled

**Solution:** Inject dependencies through constructor

```csharp
// Handler receives dependencies
app.MapGet("/api/products", async (IProductRepository repo) =>
{
    var products = await repo.GetAllAsync();
    return Results.Ok(products);
});

// Registered in DI container
builder.Services.AddScoped<IProductRepository, ProductRepository>();
```

**Benefits:**
- Loose coupling
- Easy to test
- Easy to configure

### 3. DTO Pattern (Data Transfer Objects)

**Problem:** Domain models exposed to clients

**Solution:** Use separate DTOs for requests/responses

```csharp
// Domain model (internal)
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    // ... all properties
}

// Request DTO (what client sends)
public class CreateProductRequest
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; }
}

// Response DTO (what we return)
// Mapped from domain model to DTO
```

**Benefits:**
- API contract separate from domain
- Validation at boundaries
- Clear request/response contracts

### 4. Contract Testing Pattern

**Problem:** Provider and consumer drift apart

**Solution:** Test both against shared specification

```
Spec (Single Source of Truth)
  │
  ├─ Provider must implement → Tests validate provider
  │
  └─ Consumer must use → Tests validate consumer
```

**Test Structure:**
```csharp
[Fact]  // Test validates contract
public async Task GetProducts_ShouldReturn_ValidResponse()
{
    // Act: Consumer makes valid request
    var products = await _client.GetAllProductsAsync();
    
    // Assert: Provider returns valid response
    AssertProductSchema(products);
    AssertRequiredFields(products);
}
```

## Data Flow

### GET /api/products - Happy Path

```
1. CLIENT REQUEST
   GET /api/products?skip=0&take=10
   ├─ Path: /api/products ✓ (in spec)
   └─ Query: skip, take ✓ (in spec)

2. PROVIDER RECEIVES
   app.MapGet("/api/products", async (repo, skip, take) =>
   ├─ Maps URL → Handler ✓
   ├─ Parses query params ✓
   └─ Passes to dependency ✓

3. VALIDATION
   if (skip < 0 || take < 1 || take > 100)
   ├─ Validates per spec ✓
   ├─ Boundary checks ✓
   └─ Returns 400 if invalid ✓

4. BUSINESS LOGIC
   var products = await repo.GetAllAsync(skip, take);
   ├─ Queries data ✓
   ├─ Applies pagination ✓
   └─ Returns list ✓

5. RESPONSE
   Results.Ok(products)
   ├─ Status: 200 OK ✓
   ├─ Content-Type: application/json ✓
   ├─ Schema: Product[] ✓
   └─ Required fields: [id, name, price, ...] ✓

6. CONSUMER RECEIVES
   var products = await _client.GetAllProductsAsync();
   ├─ Deserializes JSON ✓
   ├─ Maps to List<Product> ✓
   └─ Returns to caller ✓

7. TEST VALIDATES
   products.Should().BeOfType<List<Product>>();
   products.Should().NotBeEmpty();
   foreach (var p in products)
   {
       p.Id.Should().BeGreaterThan(0);        // Spec: required, int
       p.Name.Should().NotBeNullOrEmpty();    // Spec: required, string
       p.Price.Should().BeGreaterThan(0);     // Spec: required, > 0
       p.Category.Should().BeOneOf(...);      // Spec: enum validation
   }
   
   ✓ All assertions pass
   ✓ Contract validated
```

### POST /api/products - Error Path

```
1. CLIENT REQUEST
   POST /api/products
   {
     "name": "",              // Invalid: empty
     "price": -10,            // Invalid: negative
     "category": "Invalid"    // Invalid: not in enum
   }

2. PROVIDER RECEIVES & VALIDATES
   var errors = new List<ValidationError>();
   
   if (string.IsNullOrWhiteSpace(request.Name))
       errors.Add(new(...));   // ✓ Catches empty name
   
   if (request.Price <= 0)
       errors.Add(new(...));   // ✓ Catches negative price
   
   if (!IsValidCategory(request.Category))
       errors.Add(new(...));   // ✓ Catches invalid category

3. ERROR RESPONSE
   Results.UnprocessableEntity(
       new ValidationErrorResponse
       {
           Code = "VALIDATION_ERROR",
           Errors = errors
       }
   )
   
   ├─ Status: 422 Unprocessable Entity ✓
   ├─ Schema matches spec ✓
   └─ Error details included ✓

4. CONSUMER RECEIVES ERROR
   await _client.CreateProductAsync(invalidRequest)
   ├─ Throws HttpRequestException ✓
   ├─ Provides error details ✓
   └─ Consumer handles gracefully ✓

5. TEST VALIDATES
   await Assert.ThrowsAsync<HttpRequestException>(
       () => _client.CreateProductAsync(invalidRequest)
   );
   
   ✓ Error case handled
   ✓ Contract validated
```

## Error Handling Strategy

### By HTTP Status Code

```
┌─────────────────────────────────────────────────────────────┐
│                     ERROR HANDLING                           │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│ 400 Bad Request                                             │
│ ├─ Invalid parameters                                      │
│ ├─ Missing required fields                                 │
│ └─ Malformed JSON                                          │
│                                                              │
│ 404 Not Found                                               │
│ ├─ Resource doesn't exist                                  │
│ ├─ Invalid ID                                              │
│ └─ Endpoint not found                                      │
│                                                              │
│ 422 Unprocessable Entity                                    │
│ ├─ Validation errors                                       │
│ ├─ Business rule violations                                │
│ └─ Data constraints                                        │
│                                                              │
│ 500 Internal Server Error                                   │
│ ├─ Unexpected exceptions                                   │
│ ├─ Database errors                                         │
│ └─ Service dependencies down                              │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Error Response Schema

```csharp
// Spec defines error response
public class ErrorResponse
{
    public string Code { get; set; }        // Machine-readable
    public string Message { get; set; }     // Human-readable
}

public class ValidationErrorResponse
{
    public string Code { get; set; }
    public string Message { get; set; }
    public List<ValidationError> Errors { get; set; }  // Details
}

// Usage in provider
Results.NotFound(new ErrorResponse
{
    Code = "NOT_FOUND",
    Message = $"Product with ID {id} not found"
});

// Consumer handles
catch (KeyNotFoundException ex)
{
    // Handle missing resource
}
```

## Validation Strategy

### Multi-Layer Validation

```
Input Request
    │
    ├─→ 1. Parameter Validation
    │   ├─ Type checking (int, string, etc.)
    │   ├─ Range validation (min, max)
    │   └─ Pattern matching (regex)
    │
    ├─→ 2. Schema Validation
    │   ├─ Required fields present
    │   ├─ Correct JSON structure
    │   └─ No extra fields
    │
    ├─→ 3. Business Validation
    │   ├─ Enum values valid
    │   ├─ Dependencies satisfied
    │   └─ State transitions allowed
    │
    └─→ Processing
        └─ Database operations
```

### Example: Create Product

```csharp
public async Task CreateProduct_Validation(CreateProductRequest request)
{
    var errors = new List<ValidationError>();
    
    // 1. Parameter validation
    if (string.IsNullOrWhiteSpace(request.Name))
        errors.Add(new { Field = "name", Issue = "Required" });
    
    if (request.Name?.Length > 200)
        errors.Add(new { Field = "name", Issue = "Too long" });
    
    // 2. Schema validation
    if (request.Price <= 0)
        errors.Add(new { Field = "price", Issue = "Must be > 0" });
    
    // 3. Business validation
    if (!IsValidCategory(request.Category))
        errors.Add(new { Field = "category", Issue = "Invalid enum" });
    
    if (errors.Count > 0)
        return Results.UnprocessableEntity(new ValidationErrorResponse
        {
            Code = "VALIDATION_ERROR",
            Errors = errors
        });
    
    // Process if valid
    var product = await repo.CreateAsync(request);
    return Results.Created($"/api/products/{product.Id}", product);
}
```

## Testing Strategy

### Test Pyramid

```
        ╱╲
       ╱  ╲
      ╱ E2E ╲           (1 tests)
     ╱────────╲
    ╱          ╲
   ╱ Integration╲       (3 tests)
  ╱──────────────╲
 ╱                ╲
╱ Contract Tests  ╲    (19 tests)
╱──────────────────╲   ← You are here
XXXXXXXXXXXXXXXXXX

Contract Tests:
✓ Test both provider & consumer
✓ Validate against spec
✓ Fast (no database)
✓ Reliable (isolated)
✓ Comprehensive (all paths)
```

### Test Categories

```
Health Tests (1)
├─ GET /api/health → 200 "ok"

Query Tests (5)
├─ Array returned
├─ Required fields present
├─ Data types correct
├─ Enums valid
└─ Pagination works

Read Tests (3)
├─ Valid ID → 200 Product
├─ Schema matches
└─ Invalid ID → 404

Create Tests (4)
├─ Valid data → 201
├─ All fields returned
├─ Invalid price → 422
└─ Invalid category → 422

Update Tests (3)
├─ Valid update → 200
├─ All fields updated
└─ Invalid ID → 404

Delete Tests (2)
├─ Valid delete → 204
└─ Invalid ID → 404
```

## Integration with CI/CD

### Pipeline Strategy

```
┌─ Commit
│
├─ 1. Build
│  └─ dotnet build
│
├─ 2. Unit Tests
│  └─ dotnet test --filter Category=Unit
│
├─ 3. Contract Tests (⭐ You are here)
│  ├─ Start provider (docker run)
│  ├─ dotnet test
│  └─ Stop provider
│
├─ 4. Integration Tests
│  └─ dotnet test --filter Category=Integration
│
├─ 5. Deploy
│  └─ docker push
│
└─ ✓ Release
```

### Example CI Script

```yaml
# .github/workflows/contract-tests.yml
name: Contract Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0'
      
      - name: Start Provider
        run: |
          cd Provider
          dotnet build
          dotnet run &
          sleep 5
      
      - name: Run Contract Tests
        run: |
          cd Consumer
          dotnet restore
          dotnet test
```

## Best Practices Summary

| Practice | Benefit |
|----------|---------|
| **Spec First** | Alignment before implementation |
| **Single Source** | No drift between spec and code |
| **Validate Inputs** | Catch errors early |
| **Error Responses** | Clear feedback to clients |
| **Test Both Sides** | Provider AND consumer tested |
| **Automate Tests** | Run on every commit |
| **Document APIs** | Generated from spec |
| **Version Specs** | Track changes over time |

---

**Key Takeaway**: The specification is the contract. Architecture, code, and tests all validate compliance with it.
