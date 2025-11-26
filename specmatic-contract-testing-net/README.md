# Specmatic Contract Testing - .NET Example

Complete working example of API contract testing using Specmatic with .NET Core.

## Overview

This project demonstrates **spec-driven contract testing** for a .NET REST API. It includes:

- **OpenAPI 3.0 Specification**: Single source of truth for the API contract
- **ASP.NET Core Provider**: REST API implementing the specification
- **xUnit Consumer Tests**: Contract tests validating the API

## What is Specmatic?

Specmatic is an API contract testing tool based on the principle that **the specification IS the contract**.

### Philosophy
```
Traditional Testing              Specmatic Testing
├── Manual test writing          ├── Auto-generate tests from spec
├── Hard to maintain             ├── Single source of truth
├── Easy to drift from spec      └── Always in sync with spec
└── Fragile contracts
```

### Key Benefits
✅ **Spec-Driven**: OpenAPI spec is the contract  
✅ **Automatic Testing**: Generate test scenarios automatically  
✅ **Multi-Platform**: Works with any language/framework  
✅ **Consumer & Provider**: Test both sides of the contract  
✅ **Error Cases**: Automatically test edge cases  

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│              OpenAPI 3.0 Specification                       │
│         (specs/products-api.yaml - Single Source of Truth)  │
└────────────────────┬────────────────────────────────────────┘
                     │
       ┌─────────────┴──────────────┐
       │                            │
       ▼                            ▼
┌──────────────────┐        ┌────────────────────┐
│ ASP.NET Core     │        │ xUnit Consumer     │
│ Provider API     │        │ Tests              │
│ (/api/products)  │        │ (Contract Tests)   │
│                  │        │                    │
│ • Implements     │        │ • Validate spec    │
│   all endpoints  │        │   compliance       │
│ • Validates      │        │ • Test requests/   │
│   requests       │        │   responses        │
│ • Returns        │        │ • Verify schemas   │
│   responses      │        │ • Check HTTP codes │
└────────┬─────────┘        └────────────────────┘
         │                          │
         └──────────────┬───────────┘
                        │
                        ▼
              ┌────────────────────┐
              │  Contract Test     │
              │  Results           │
              │                    │
              │ ✓ 19 tests pass    │
              │ ✓ All endpoints    │
              │   validated        │
              │ ✓ All schemas ok   │
              │ ✓ Ready to deploy  │
              └────────────────────┘
```

## Project Structure

```
specmatic-contract-testing-net/
│
├── specs/
│   └── products-api.yaml              # OpenAPI specification
│
├── Provider/                           # ASP.NET Core API
│   ├── src/
│   │   ├── Program.cs                 # Startup & endpoints
│   │   ├── Models.cs                  # Domain models
│   │   └── ProductRepository.cs       # Data access
│   ├── provider.csproj
│   └── appsettings.json
│
├── Consumer/                           # Test client
│   ├── src/
│   │   ├── Models.cs                  # Shared models
│   │   └── ProductApiClient.cs        # HTTP client
│   ├── tests/
│   │   └── ProductApiContractTests.cs # xUnit tests
│   └── consumer.csproj
│
├── Dockerfile                          # Provider container image
├── docker-compose.yml                  # Local deployment
├── specmatic.yaml                      # Specmatic config
└── README.md                           # This file
```

## Component Details

### 1. OpenAPI Specification (`specs/products-api.yaml`)

Defines the complete API contract:

```yaml
paths:
  /api/products:
    get:
      summary: Get all products
      responses:
        '200':
          schema:
            type: array
            items:
              $ref: '#/components/schemas/Product'
    post:
      summary: Create product
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/CreateProductRequest'
      responses:
        '201':
          schema:
            $ref: '#/components/schemas/Product'

components:
  schemas:
    Product:
      type: object
      required: [id, name, price, inStock, createdAt]
      properties:
        id: { type: integer }
        name: { type: string }
        price: { type: number, minimum: 0 }
        category:
          type: string
          enum: [Electronics, Clothing, Books, Home, Other]
```

### 2. Provider Implementation (`Provider/`)

ASP.NET Core REST API implementing the specification:

```csharp
// Endpoint matches spec
app.MapGet("/api/products", async (IProductRepository repo, int skip = 0, int take = 10) =>
{
    // Validate per spec
    if (skip < 0 || take < 1 || take > 100)
        return Results.BadRequest(...);
    
    // Return response per spec
    var products = await repo.GetAllAsync(skip, take);
    return Results.Ok(products);
});

// Implements spec schema
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 3. Consumer Client (`Consumer/src/ProductApiClient.cs`)

HTTP client using the API per specification:

```csharp
public class ProductApiClient
{
    public async Task<List<Product>> GetAllProductsAsync(int skip = 0, int take = 10)
    {
        // Request matches spec
        var url = $"{_baseUrl}/api/products?skip={skip}&take={take}";
        var response = await _httpClient.GetAsync(url);
        
        // Handle response per spec
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(...);
        
        // Parse response per spec schema
        var content = await response.Content.ReadAsStringAsync();
        var products = JsonSerializer.Deserialize<List<Product>>(content);
        return products;
    }
}
```

### 4. Contract Tests (`Consumer/tests/ProductApiContractTests.cs`)

xUnit tests validating both provider and consumer:

```csharp
public class ProductApiContractTests
{
    [Fact]
    public async Task GetAllProducts_ShouldReturn_ArrayOfProducts()
    {
        // Validate request per spec (client can make it)
        var products = await _client.GetAllProductsAsync();
        
        // Validate response per spec (API returns it correctly)
        products.Should().BeOfType<List<Product>>();
    }
    
    [Fact]
    public async Task GetAllProducts_ShouldReturn_ProductsWithRequiredFields()
    {
        // Spec says: id, name, price, inStock, createdAt are required
        var products = await _client.GetAllProductsAsync();
        
        foreach (var product in products)
        {
            product.Id.Should().BeGreaterThan(0);
            product.Name.Should().NotBeNullOrEmpty();
            product.Price.Should().BeGreaterThan(0);
            product.CreatedAt.Should().NotBe(default);
        }
    }
}
```

## Testing Workflow

### Step 1: Define Contract
```yaml
# specs/products-api.yaml
GET /api/products/{id}
  - Expects: id (integer, >= 1)
  - Returns: 200 Product | 404 Error
```

### Step 2: Implement Provider
```csharp
// Provider/src/Program.cs
app.MapGet("/api/products/{id}", async (int id, IProductRepository repo) =>
{
    if (id < 1)
        return Results.BadRequest(...);
    
    var product = await repo.GetByIdAsync(id);
    if (product == null)
        return Results.NotFound(...);
    
    return Results.Ok(product);
});
```

### Step 3: Implement Consumer
```csharp
// Consumer/src/ProductApiClient.cs
public async Task<Product> GetProductByIdAsync(int id)
{
    var response = await _httpClient.GetAsync($"{_baseUrl}/api/products/{id}");
    
    if (response.StatusCode == HttpStatusCode.NotFound)
        throw new KeyNotFoundException();
    
    return JsonSerializer.Deserialize<Product>(await response.Content.ReadAsStringAsync());
}
```

### Step 4: Write Contract Tests
```csharp
// Consumer/tests/ProductApiContractTests.cs
[Fact]
public async Task GetProductById_WithValidId_ShouldReturn_Product()
{
    var products = await _client.GetAllProductsAsync();
    var product = await _client.GetProductByIdAsync(products.First().Id);
    product.Should().NotBeNull();
}

[Fact]
public async Task GetProductById_WithInvalidId_ShouldThrow_KeyNotFoundException()
{
    await Assert.ThrowsAsync<KeyNotFoundException>(
        () => _client.GetProductByIdAsync(9999));
}
```

### Step 5: Run Tests
```bash
cd Consumer
dotnet test
```

## Test Coverage

### Health Check (1 test)
- ✅ GET /api/health returns "ok"

### GET /api/products (5 tests)
- ✅ Returns array of products
- ✅ Products have required fields
- ✅ Correct data types
- ✅ Valid category enum
- ✅ Pagination works

### GET /api/products/{id} (3 tests)
- ✅ Valid ID returns product
- ✅ Correct schema
- ✅ Invalid ID returns 404

### POST /api/products (4 tests)
- ✅ Valid data creates product
- ✅ Returns all required fields
- ✅ Invalid price rejected
- ✅ Invalid category rejected

### PUT /api/products/{id} (3 tests)
- ✅ Valid update succeeds
- ✅ All fields updated
- ✅ Non-existent returns 404

### DELETE /api/products/{id} (2 tests)
- ✅ Existing product deleted
- ✅ Non-existent returns 404

**Total: 19 tests, all validating contract compliance**

## Running the Example

### Local Development

```bash
# Terminal 1: Start Provider
cd Provider
dotnet restore
dotnet run
# Output: Now listening on http://localhost:5000

# Terminal 2: Run Tests
cd Consumer
dotnet restore
dotnet test
```

### Docker Deployment

```bash
# Build and run provider in container
docker-compose up --build

# In another terminal: Run tests
cd Consumer
dotnet test
```

## Key Concepts

### Single Source of Truth
The OpenAPI specification is **the** contract. All testing validates against it.

### Provider Responsibility
Provider must implement the spec exactly:
- All endpoints defined
- All parameters accepted
- All schemas returned
- All status codes returned

### Consumer Responsibility
Consumer must use the spec correctly:
- Valid requests only
- Handle all status codes
- Parse responses correctly
- Handle errors per spec

### Contract Testing
Tests verify the contract:
- Consumer can make valid requests
- Provider returns correct responses
- Both follow the spec
- Integration works

## Best Practices

1. **Spec First**
   - Define spec before implementation
   - Team agrees on spec first
   - Implementation follows spec

2. **Keep Tests Updated**
   - Update tests when spec changes
   - Don't skip failing tests
   - Track test coverage

3. **Comprehensive Coverage**
   - Happy paths (200, 201)
   - Error cases (400, 404)
   - Edge cases (boundaries, enums)
   - Validation (required fields)

4. **Automate**
   - CI/CD integration
   - Run on every commit
   - Block on failures
   - Report results

5. **Maintain**
   - Review tests regularly
   - Update documentation
   - Refactor as needed
   - Keep tests fast

## Common Issues & Solutions

### Port Already in Use
```bash
# Find process on port 5000
netstat -ano | findstr :5000

# Kill process (Windows)
taskkill /PID <PID> /F
```

### Tests Timeout
```bash
# Increase timeout in test
[Fact(Timeout = 5000)]
public async Task MyTest() { ... }
```

### Model Deserialization Issues
```csharp
// Ensure JsonSerializerOptions match
var options = new JsonSerializerOptions 
{ 
    PropertyNameCaseInsensitive = true 
};
var model = JsonSerializer.Deserialize<Product>(json, options);
```

## Technology Stack

| Component | Technology | Version |
|-----------|-----------|---------|
| Runtime | .NET | 8.0 |
| Web Framework | ASP.NET Core | 8.0 |
| Testing | xUnit | 2.6 |
| Assertions | FluentAssertions | 6.12 |
| Container | Docker | Latest |

## Resources

- [OpenAPI Specification](https://spec.openapis.org) - API specification standard
- [ASP.NET Core Docs](https://docs.microsoft.com/aspnet/core) - Framework documentation
- [xUnit Documentation](https://xunit.net) - Testing framework
- [Specmatic Documentation](https://specmatic.io) - Contract testing tool
- [RESTful API Best Practices](https://restfulapi.net) - API design patterns

## Next Steps

1. ✅ Run the example end-to-end
2. 🔍 Explore the OpenAPI spec
3. ✏️ Modify an endpoint
4. 🔄 Update the tests
5. 📊 Integrate into CI/CD

## Summary

This example demonstrates:
- ✅ Specification-driven API development
- ✅ Contract-based testing with xUnit
- ✅ Provider and consumer validation
- ✅ ASP.NET Core best practices
- ✅ Docker containerization
- ✅ Complete test coverage

The specification is the contract. The tests verify both sides follow it.

---

**Last Updated**: November 26, 2025
